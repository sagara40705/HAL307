using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public sealed class GamePhaseManager : AppManagerBase<GamePhaseManager>
{
    private const string DebugWindowName = "Game Phase";

    private GamePhaseSceneSettings _settings;
    private GamePhase _currentPhase = GamePhase.None;
    private GamePhase _previousPhase = GamePhase.None;
    private GamePhase _phaseBeforePause = GamePhase.None;
    private bool _isChangingPhase;

    public GamePhase CurrentPhase => _currentPhase;
    public GamePhase PreviousPhase => _previousPhase;
    public bool IsChangingPhase => _isChangingPhase;

    public event Action<GamePhase, GamePhase> PhaseChanged;

    protected override IEnumerator OnInitialize()
    {
        _settings = GamePhaseSceneSettings.Load();
        if (_settings == null)
        {
            Debug.LogError($"[GamePhaseManager] Initialize failed. Reason={GamePhaseSceneSettings.ResourcesName} was not found in Resources.");
            FailInitialize("GamePhaseSceneSettings was not found.");
            yield break;
        }

        _currentPhase = ResolvePhaseFromActiveScene();
        _previousPhase = GamePhase.None;
        _phaseBeforePause = GamePhase.None;
        _isChangingPhase = false;

        Debug.Log($"[GamePhaseManager] Initialize completed. CurrentPhase={_currentPhase}, ActiveScene='{UnitySceneManager.GetActiveScene().name}'");
        CreateDebugMenu();
        yield break;
    }

    protected override void OnDestroy()
    {
        RuntimeDebug.DebugMenu.DestroyWindow(DebugWindowName);
        base.OnDestroy();
    }

    public bool ChangePhase(GamePhase nextPhase)
    {
        if (!CanStartPhaseChange(nextPhase))
        {
            return false;
        }

        StartCoroutine(CoroutineUtility.Run(ChangePhaseRoutine(nextPhase)));
        return true;
    }

    public bool LoadSceneAndSyncPhase(string sceneName)
    {
        if (!IsInitialized)
        {
            Debug.LogWarning($"[GamePhaseManager] Scene load skipped. Reason=GamePhaseManager is not initialized. Scene='{sceneName}'");
            return false;
        }

        if (_isChangingPhase)
        {
            Debug.LogWarning($"[GamePhaseManager] Scene load skipped. Reason=Already changing phase. Scene='{sceneName}', CurrentPhase={_currentPhase}");
            return false;
        }

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[GamePhaseManager] Scene load failed. Reason=Scene name is empty.");
            return false;
        }

        StartCoroutine(CoroutineUtility.Run(LoadSceneAndSyncPhaseRoutine(sceneName)));
        return true;
    }

    public IEnumerator ChangePhaseAsync(GamePhase nextPhase)
    {
        if (!CanStartPhaseChange(nextPhase))
        {
            yield break;
        }

        // Scene遷移で呼び出し元が破棄されてもManager側でPhase同期まで継続する
        yield return StartCoroutine(CoroutineUtility.Run(ChangePhaseRoutine(nextPhase)));
    }

    public bool ResumePreviousPhase()
    {
        GamePhase resumePhase = _phaseBeforePause != GamePhase.None ? _phaseBeforePause : _previousPhase;
        if (resumePhase == GamePhase.None || resumePhase == GamePhase.Pause)
        {
            Debug.LogWarning("[GamePhaseManager] ResumePreviousPhase skipped. Reason=Previous phase is empty.");
            return false;
        }

        return ChangePhase(resumePhase);
    }

    public bool TryGetPhaseForSceneName(string sceneName, out GamePhase phase)
    {
        if (_settings == null)
        {
            _settings = GamePhaseSceneSettings.Load();
        }

        if (_settings == null)
        {
            phase = GamePhase.None;
            return false;
        }

        return _settings.TryGetPhaseBySceneName(sceneName, out phase);
    }

    public void SyncPhaseWithActiveScene()
    {
        Scene activeScene = UnitySceneManager.GetActiveScene();
        string sceneName = string.IsNullOrWhiteSpace(activeScene.path) ? activeScene.name : activeScene.path;
        SyncPhaseBySceneName(sceneName);
    }

    public void SyncPhaseBySceneName(string sceneName)
    {
        GamePhase nextPhase = ResolvePhaseFromSceneName(sceneName);
        SetCurrentPhase(nextPhase, _currentPhase);
    }

    private IEnumerator LoadSceneAndSyncPhaseRoutine(string sceneName)
    {
        _isChangingPhase = true;
        try
        {
            Debug.Log($"[GamePhaseManager] Scene load started. Scene='{sceneName}', CurrentPhase={_currentPhase}");

            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[GamePhaseManager] Scene load failed. Reason=Scene cannot be loaded. Scene='{sceneName}'. Check Build Settings scene list or Build Profile scene list.");
                yield break;
            }

            SceneLoader sceneLoader = SceneLoader.Instance;
            if (sceneLoader == null || !sceneLoader.IsInitialized)
            {
                Debug.LogError($"[GamePhaseManager] Scene load failed. Reason=SceneLoader is not ready. Scene='{sceneName}'");
                yield break;
            }

            yield return LoadSceneWithPhaseCleanup(sceneLoader, sceneName);

            SyncPhaseWithActiveScene();

            Scene activeScene = UnitySceneManager.GetActiveScene();
            if (!sceneLoader.IsSceneLoaded(sceneName))
            {
                Debug.LogError($"[GamePhaseManager] Scene load failed. Reason=Requested scene is not loaded. RequestedScene='{sceneName}', ActiveScene='{activeScene.name}', CurrentPhase={_currentPhase}");
                yield break;
            }

            Debug.Log($"[GamePhaseManager] Scene load completed. RequestedScene='{sceneName}', ActiveScene='{activeScene.name}', CurrentPhase={_currentPhase}");
        }
        finally
        {
            _isChangingPhase = false;
        }
    }

    private IEnumerator ChangePhaseRoutine(GamePhase nextPhase)
    {
        _isChangingPhase = true;
        try
        {
            GamePhase oldPhase = _currentPhase;
            Debug.Log($"[GamePhaseManager] ChangePhase started. From={oldPhase}, To={nextPhase}");

            if (nextPhase == GamePhase.Pause)
            {
                yield return OpenPausePhase(oldPhase);
            }
            else
            {
                if (oldPhase == GamePhase.Pause)
                {
                    yield return ClosePausePhase();

                    if (nextPhase == _phaseBeforePause && IsActiveSceneForPhase(nextPhase))
                    {
                        SetCurrentPhase(nextPhase, oldPhase);
                        yield break;
                    }
                }

                yield return LoadPhaseScene(nextPhase);
            }
        }
        finally
        {
            _isChangingPhase = false;
        }
    }

    private IEnumerator OpenPausePhase(GamePhase oldPhase)
    {
        PauseManager pauseManager = PauseManager.Instance;
        if (pauseManager == null || !pauseManager.IsInitialized)
        {
            Debug.LogError("[GamePhaseManager] Pause phase change failed. Reason=PauseManager is not ready.");
            yield break;
        }

        if (_currentPhase != GamePhase.Pause)
        {
            _phaseBeforePause = _currentPhase;
        }

        yield return pauseManager.OpenPauseAsync();

        if (!pauseManager.IsPaused)
        {
            Debug.LogError("[GamePhaseManager] Pause phase change failed. Reason=PauseManager did not enter pause.");
            yield break;
        }

        SetCurrentPhase(GamePhase.Pause, oldPhase);
    }

    private IEnumerator ClosePausePhase()
    {
        PauseManager pauseManager = PauseManager.Instance;
        if (pauseManager == null || !pauseManager.IsInitialized || !pauseManager.IsPaused)
        {
            yield break;
        }

        yield return pauseManager.ClosePauseAsync();
    }

    private IEnumerator LoadPhaseScene(GamePhase nextPhase)
    {
        if (!_settings.TryGetSceneName(nextPhase, out string sceneName))
        {
            Debug.LogError($"[GamePhaseManager] ChangePhase failed. Reason=Scene is not configured. Phase={nextPhase}");
            yield break;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[GamePhaseManager] ChangePhase failed. Reason=Scene cannot be loaded. Phase={nextPhase}, Scene='{sceneName}'. Check Build Settings scene list.");
            yield break;
        }

        SceneLoader sceneLoader = SceneLoader.Instance;
        if (sceneLoader == null || !sceneLoader.IsInitialized)
        {
            Debug.LogError($"[GamePhaseManager] ChangePhase failed. Reason=SceneLoader is not ready. Phase={nextPhase}, Scene='{sceneName}'");
            yield break;
        }

        yield return LoadSceneWithPhaseCleanup(sceneLoader, sceneName);
        SyncPhaseWithActiveScene();

        if (_currentPhase != nextPhase)
        {
            Debug.LogError($"[GamePhaseManager] ChangePhase failed. Reason=Active scene did not resolve to the requested phase. RequestedPhase={nextPhase}, Scene='{sceneName}', ActiveScene='{UnitySceneManager.GetActiveScene().name}', ResolvedPhase={_currentPhase}");
            yield break;
        }
    }

    // 実シーン遷移のみ旧フェーズの音を停止。Pauseの開閉・復帰では呼ばない
    private IEnumerator LoadSceneWithPhaseCleanup(SceneLoader sceneLoader, string sceneName)
    {
        SoundManager soundManager = SoundManager.Instance;

        void ResumePlayback()
        {
            if (soundManager != null)
            {
                soundManager.EndSceneTransition();
            }
            Sound.Manager.SoundManager.EndSceneTransition();
        }

        // 遷移先のStartで再生される音を止めないよう、ロード通知で再生を解禁
        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Single)
            {
                ResumePlayback();
            }
        }

        UnitySceneManager.sceneLoaded += OnSceneLoaded;
        try
        {
            if (soundManager != null)
            {
                soundManager.BeginSceneTransition();
            }
            Sound.Manager.SoundManager.BeginSceneTransition();
            yield return sceneLoader.LoadAsync(sceneName);
        }
        finally
        {
            UnitySceneManager.sceneLoaded -= OnSceneLoaded;
            ResumePlayback();
        }
    }

    private bool CanStartPhaseChange(GamePhase nextPhase)
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("[GamePhaseManager] ChangePhase skipped. Reason=GamePhaseManager is not initialized.");
            return false;
        }

        if (_isChangingPhase)
        {
            Debug.LogWarning($"[GamePhaseManager] ChangePhase skipped. Reason=Already changing phase. CurrentPhase={_currentPhase}, NextPhase={nextPhase}");
            return false;
        }

        if (nextPhase == GamePhase.None)
        {
            Debug.LogError("[GamePhaseManager] ChangePhase failed. Reason=Next phase is None.");
            return false;
        }

        if (_currentPhase == nextPhase)
        {
            if (nextPhase != GamePhase.Pause)
            {
                return false;
            }

            PauseManager pauseManager = PauseManager.Instance;
            if (pauseManager != null && pauseManager.IsPaused)
            {
                return false;
            }
        }

        return true;
    }

    private void SetCurrentPhase(GamePhase nextPhase, GamePhase oldPhase)
    {
        if (_currentPhase == nextPhase)
        {
            return;
        }

        _previousPhase = oldPhase;
        _currentPhase = nextPhase;
        Debug.Log($"[GamePhaseManager] Phase changed. From={oldPhase}, To={nextPhase}");
        PhaseChanged?.Invoke(oldPhase, nextPhase);
    }

    private void DebugChangePhase(GamePhase nextPhase)
    {
        if (!ChangePhase(nextPhase))
        {
            Debug.LogWarning($"[GamePhaseManager] Debug ChangePhase skipped. CurrentPhase={_currentPhase}, NextPhase={nextPhase}");
        }
    }

    private void CreateDebugMenu()
    {
        RuntimeDebug.DebugWindow window = RuntimeDebug.DebugMenu.CreateWindow(DebugWindowName);
        window.CreateMessage("Current Phase", () => _currentPhase.ToString());
        window.CreateMessage("Previous Phase", () => _previousPhase.ToString());
        window.CreateMessage("Changing Phase", () => _isChangingPhase.ToString());
        window.CreateButton("Change Title", () => DebugChangePhase(GamePhase.Title));
        window.CreateButton("Change InGame", () => DebugChangePhase(GamePhase.InGame));
        window.CreateButton("Change Result", () => DebugChangePhase(GamePhase.Result));
        window.CreateButton("Change Pause", () => DebugChangePhase(GamePhase.Pause));
    }

    private bool IsActiveSceneForPhase(GamePhase phase)
    {
        if (_settings == null || !_settings.TryGetSceneName(phase, out string sceneName))
        {
            return false;
        }

        return SceneLookup.Matches(UnitySceneManager.GetActiveScene(), sceneName);
    }

    private GamePhase ResolvePhaseFromActiveScene()
    {
        Scene activeScene = UnitySceneManager.GetActiveScene();
        string activeSceneName = string.IsNullOrWhiteSpace(activeScene.path) ? activeScene.name : activeScene.path;
        return ResolvePhaseFromSceneName(activeSceneName);
    }

    private GamePhase ResolvePhaseFromSceneName(string sceneName)
    {
        if (_settings != null && _settings.TryGetPhaseBySceneName(sceneName, out GamePhase phase))
        {
            return phase;
        }

        return GamePhase.None;
    }
}
