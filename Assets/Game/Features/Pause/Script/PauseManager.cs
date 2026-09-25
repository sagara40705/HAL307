using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PauseManager : AppManagerBase<PauseManager>
{
    [SerializeField] private string pauseSceneName = "Pause";
    [SerializeField] private bool preloadPauseSceneOnInitialize = true;

    private VoiceMainMenuController _pauseMenuController;
    private float _previousTimeScale = 1f;
    private bool _isPaused;
    private bool _isTransitioning;
    private bool _isApplyingPauseView;
    private bool _isPreloading;

    public bool IsPaused => _isPaused;
    public bool IsApplyingPauseView => _isApplyingPauseView;

    protected override IEnumerator OnInitialize()
    {
        Time.timeScale = 1f;
        _previousTimeScale = 1f;
        _isPaused = false;
        _isTransitioning = false;
        _isApplyingPauseView = false;
        _isPreloading = false;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        if (preloadPauseSceneOnInitialize)
        {
            yield return EnsurePauseSceneLoaded("Initialize");

            if (IsPauseSceneLoaded())
            {
                SetPauseViewActive(false);
            }
        }
    }

    public void OpenPause()
    {
        StartCoroutine(OpenPauseAsync());
    }

    public IEnumerator OpenPauseAsync()
    {
        if (!CanStartPauseOperation())
        {
            yield break;
        }

        if (_isPaused)
        {
            yield break;
        }

        yield return StartCoroutine(CoroutineUtility.Run(OpenPauseCoroutine()));
    }

    public void ClosePause()
    {
        StartCoroutine(ClosePauseAsync());
    }

    public IEnumerator ClosePauseAsync()
    {
        if (!CanStartPauseOperation())
        {
            yield break;
        }

        if (!_isPaused)
        {
            yield break;
        }

        yield return StartCoroutine(CoroutineUtility.Run(ClosePauseCoroutine()));
    }

    protected override void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (ReferenceEquals(Instance, this))
        {
            Time.timeScale = 1f;
        }

        base.OnDestroy();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsInitialized)
        {
            return;
        }

        if (SceneLookup.Matches(scene, pauseSceneName))
        {
            return;
        }

        if (mode == LoadSceneMode.Single)
        {
            _pauseMenuController = null;
            _isPaused = false;
            _isTransitioning = false;
            RestoreTimeScale();
        }

        if (preloadPauseSceneOnInitialize && !IsPauseSceneLoaded())
        {
            StartCoroutine(CoroutineUtility.Run(PreloadPauseSceneAfterSceneLoad()));
        }
    }

    private IEnumerator OpenPauseCoroutine()
    {
        _isTransitioning = true;
        bool changedTimeScale = false;
        try
        {
            yield return EnsurePauseSceneLoaded("OpenPause");

            if (!IsPauseSceneLoaded())
            {
                Debug.LogError($"[PauseManager] OpenPause failed. Reason=Pause scene is not loaded. Scene='{pauseSceneName}'");
                yield break;
            }

            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            changedTimeScale = true;

            if (!SetPauseViewActive(true))
            {
                yield break;
            }

            _isPaused = true;
        }
        finally
        {
            if (changedTimeScale && !_isPaused)
            {
                RestoreTimeScale();
            }

            _isTransitioning = false;
        }
    }

    private IEnumerator ClosePauseCoroutine()
    {
        _isTransitioning = true;
        try
        {
            SetPauseViewActive(false);
            RestoreTimeScale();

            _isPaused = false;
            yield break;
        }
        finally
        {
            _isTransitioning = false;
        }
    }

    private IEnumerator EnsurePauseSceneLoaded(string operationName)
    {
        if (IsPauseSceneLoaded())
        {
            yield break;
        }

        SceneLoader sceneLoader = SceneLoader.Instance;
        if (sceneLoader == null || !sceneLoader.IsInitialized)
        {
            Debug.LogError($"[PauseManager] {operationName} failed. Reason=SceneLoader is not ready. Scene='{pauseSceneName}'");
            yield break;
        }

        yield return sceneLoader.LoadAdditiveAsync(pauseSceneName);

        if (!IsPauseSceneLoaded())
        {
            Debug.LogError($"[PauseManager] {operationName} failed. Reason=Pause scene was not loaded. Scene='{pauseSceneName}'");
        }
    }

    private IEnumerator PreloadPauseSceneAfterSceneLoad()
    {
        if (_isPreloading)
        {
            yield break;
        }

        _isPreloading = true;
        try
        {
            yield return EnsurePauseSceneLoaded("Preload");

            if (IsPauseSceneLoaded() && !_isPaused && !_isTransitioning)
            {
                SetPauseViewActive(false);
            }
        }
        finally
        {
            _isPreloading = false;
        }
    }

    private bool SetPauseViewActive(bool active)
    {
        if (!TryResolvePauseMenuController(out VoiceMainMenuController controller))
        {
            return false;
        }

        _isApplyingPauseView = true;

        try
        {
            if (active)
            {
                controller.OpenMenu();
            }
            else
            {
                controller.CloseMenu();
            }
        }
        finally
        {
            _isApplyingPauseView = false;
        }

        return true;
    }

    private bool TryResolvePauseMenuController(out VoiceMainMenuController controller)
    {
        if (_pauseMenuController != null)
        {
            controller = _pauseMenuController;
            return true;
        }

        Scene pauseScene = SceneLookup.Find(pauseSceneName);
        if (!pauseScene.IsValid() || !pauseScene.isLoaded)
        {
            Debug.LogError($"[PauseManager] Pause view operation failed. Reason=Pause scene is not loaded. Scene='{pauseSceneName}'");
            controller = null;
            return false;
        }

        foreach (GameObject root in pauseScene.GetRootGameObjects())
        {
            controller = root.GetComponentInChildren<VoiceMainMenuController>(true);
            if (controller != null)
            {
                _pauseMenuController = controller;
                return true;
            }
        }

        Debug.LogError($"[PauseManager] Pause view operation failed. Reason=VoiceMainMenuController was not found. Scene='{pauseSceneName}'");
        controller = null;
        return false;
    }

    private bool CanStartPauseOperation()
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("[PauseManager] Pause operation skipped. Reason=PauseManager is not initialized.");
            return false;
        }

        if (_isTransitioning)
        {
            Debug.LogWarning("[PauseManager] Pause operation skipped. Reason=PauseManager is transitioning.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(pauseSceneName))
        {
            Debug.LogError("[PauseManager] Pause operation failed. Reason=Pause scene name is empty.");
            return false;
        }

        return true;
    }

    private bool IsPauseSceneLoaded()
    {
        Scene scene = SceneLookup.Find(pauseSceneName);
        return scene.IsValid() && scene.isLoaded;
    }

    private void RestoreTimeScale()
    {
        Time.timeScale = Mathf.Approximately(_previousTimeScale, 0f) ? 1f : _previousTimeScale;
    }
}
