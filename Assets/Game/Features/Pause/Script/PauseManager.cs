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
        if (!CanStartPauseOperation())
        {
            return;
        }

        if (_isPaused)
        {
            return;
        }

        StartCoroutine(OpenPauseCoroutine());
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

        yield return OpenPauseCoroutine();
    }

    public void ClosePause()
    {
        if (!CanStartPauseOperation())
        {
            return;
        }

        if (!_isPaused)
        {
            return;
        }

        StartCoroutine(ClosePauseCoroutine());
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

        yield return ClosePauseCoroutine();
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
        if (!preloadPauseSceneOnInitialize || !IsInitialized)
        {
            return;
        }

        if (scene.name == pauseSceneName)
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

        if (!IsPauseSceneLoaded())
        {
            StartCoroutine(PreloadPauseSceneAfterSceneLoad());
        }
    }

    private IEnumerator OpenPauseCoroutine()
    {
        _isTransitioning = true;

        yield return EnsurePauseSceneLoaded("OpenPause");

        if (!IsPauseSceneLoaded())
        {
            Debug.LogError($"[PauseManager] OpenPause failed. Reason=Pause scene is not loaded. Scene='{pauseSceneName}'");
            _isTransitioning = false;
            yield break;
        }

        _previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        if (!SetPauseViewActive(true))
        {
            RestoreTimeScale();
            _isTransitioning = false;
            yield break;
        }

        _isPaused = true;
        _isTransitioning = false;
    }

    private IEnumerator ClosePauseCoroutine()
    {
        _isTransitioning = true;

        SetPauseViewActive(false);
        RestoreTimeScale();

        _isPaused = false;
        _isTransitioning = false;
        yield break;
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

        yield return StartCoroutine(sceneLoader.LoadAdditiveAsync(pauseSceneName));

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

        yield return EnsurePauseSceneLoaded("Preload");

        if (IsPauseSceneLoaded())
        {
            SetPauseViewActive(false);
        }

        _isPreloading = false;
    }

    private bool SetPauseViewActive(bool active)
    {
        if (!active)
        {
            SetPauseSceneCamerasEnabled(false);
        }

        if (!TryResolvePauseMenuController(out VoiceMainMenuController controller))
        {
            return false;
        }

        if (active)
        {
            SetPauseSceneCamerasEnabled(true);
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

    private void SetPauseSceneCamerasEnabled(bool enabled)
    {
        Scene pauseScene = SceneManager.GetSceneByName(pauseSceneName);
        if (!pauseScene.IsValid() || !pauseScene.isLoaded)
        {
            return;
        }

        foreach (GameObject root in pauseScene.GetRootGameObjects())
        {
            Camera[] cameras = root.GetComponentsInChildren<Camera>(true);
            foreach (Camera camera in cameras)
            {
                camera.enabled = enabled;
            }
        }
    }

    private bool TryResolvePauseMenuController(out VoiceMainMenuController controller)
    {
        if (_pauseMenuController != null)
        {
            controller = _pauseMenuController;
            return true;
        }

        Scene pauseScene = SceneManager.GetSceneByName(pauseSceneName);
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
        Scene scene = SceneManager.GetSceneByName(pauseSceneName);
        return scene.IsValid() && scene.isLoaded;
    }

    private void RestoreTimeScale()
    {
        Time.timeScale = Mathf.Approximately(_previousTimeScale, 0f) ? 1f : _previousTimeScale;
    }
}
