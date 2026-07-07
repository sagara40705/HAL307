using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PauseManager : AppManagerBase<PauseManager>
{
    [SerializeField] private string pauseSceneName = "Pause";

    private float _previousTimeScale = 1f;
    private bool _isPaused;
    private bool _isTransitioning;

    protected override IEnumerator OnInitialize()
    {
        Time.timeScale = 1f;
        _isPaused = false;
        _isTransitioning = false;
        yield break;
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

    protected override void OnDestroy()
    {
        if (ReferenceEquals(Instance, this))
        {
            Time.timeScale = 1f;
        }

        base.OnDestroy();
    }

    private IEnumerator OpenPauseCoroutine()
    {
        _isTransitioning = true;
        _previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        SceneLoader sceneLoader = SceneLoader.Instance;
        if (sceneLoader == null || !sceneLoader.IsInitialized)
        {
            Debug.LogError($"[PauseManager] OpenPause failed. Reason=SceneLoader is not ready. Scene='{pauseSceneName}'");
            RestoreTimeScale();
            _isTransitioning = false;
            yield break;
        }

        if (!IsPauseSceneLoaded())
        {
            yield return StartCoroutine(sceneLoader.LoadAdditiveAsync(pauseSceneName));
        }

        if (!IsPauseSceneLoaded())
        {
            Debug.LogError($"[PauseManager] OpenPause failed. Reason=Pause scene was not loaded. Scene='{pauseSceneName}'");
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

        SceneLoader sceneLoader = SceneLoader.Instance;
        if (sceneLoader == null || !sceneLoader.IsInitialized)
        {
            Debug.LogError($"[PauseManager] ClosePause failed. Reason=SceneLoader is not ready. Scene='{pauseSceneName}'");
            RestoreTimeScale();
            _isPaused = false;
            _isTransitioning = false;
            yield break;
        }

        if (IsPauseSceneLoaded())
        {
            yield return StartCoroutine(sceneLoader.UnloadAsync(pauseSceneName));
        }

        if (IsPauseSceneLoaded())
        {
            Debug.LogError($"[PauseManager] ClosePause failed. Reason=Pause scene is still loaded. Scene='{pauseSceneName}'");
            Time.timeScale = 0f;
            _isTransitioning = false;
            yield break;
        }

        RestoreTimeScale();
        _isPaused = false;
        _isTransitioning = false;
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
