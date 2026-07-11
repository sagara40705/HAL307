using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public sealed class GamePhaseManager : AppManagerBase<GamePhaseManager>
{
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
        yield break;
    }

    public bool ChangePhase(GamePhase nextPhase)
    {
        if (!CanStartPhaseChange(nextPhase))
        {
            return false;
        }

        StartCoroutine(ChangePhaseRoutine(nextPhase));
        return true;
    }

    public IEnumerator ChangePhaseAsync(GamePhase nextPhase)
    {
        if (!CanStartPhaseChange(nextPhase))
        {
            yield break;
        }

        yield return ChangePhaseRoutine(nextPhase);
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

    private IEnumerator ChangePhaseRoutine(GamePhase nextPhase)
    {
        _isChangingPhase = true;

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

        _isChangingPhase = false;
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

        yield return sceneLoader.LoadAsync(sceneName);

        if (UnitySceneManager.GetActiveScene().name != sceneName)
        {
            Debug.LogError($"[GamePhaseManager] ChangePhase failed. Reason=Loaded scene is not active. Phase={nextPhase}, Scene='{sceneName}', ActiveScene='{UnitySceneManager.GetActiveScene().name}'");
            yield break;
        }

        SetCurrentPhase(nextPhase, _currentPhase);
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

    private bool IsActiveSceneForPhase(GamePhase phase)
    {
        if (_settings == null || !_settings.TryGetSceneName(phase, out string sceneName))
        {
            return false;
        }

        return string.Equals(UnitySceneManager.GetActiveScene().name, sceneName, StringComparison.Ordinal);
    }

    private GamePhase ResolvePhaseFromActiveScene()
    {
        string activeSceneName = UnitySceneManager.GetActiveScene().name;
        if (_settings != null && _settings.TryGetPhaseBySceneName(activeSceneName, out GamePhase phase))
        {
            return phase;
        }

        return GamePhase.None;
    }
}
