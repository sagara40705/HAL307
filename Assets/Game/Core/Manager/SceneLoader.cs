using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public sealed class SceneLoader : AppManagerBase<SceneLoader>
{
    public bool IsBusy { get; private set; }

    public string ActiveSceneName => UnitySceneManager.GetActiveScene().name;

    protected override IEnumerator OnInitialize()
    {
        IsBusy = false;
        yield break;
    }

    public IEnumerator LoadAsync(string sceneName)
    {
        yield return LoadInternalAsync(sceneName, LoadSceneMode.Single);
    }

    public IEnumerator LoadAdditiveAsync(string sceneName)
    {
        yield return LoadInternalAsync(sceneName, LoadSceneMode.Additive);
    }

    public IEnumerator UnloadAsync(string sceneName)
    {
        if (!IsInitialized)
        {
            Debug.LogError("[SceneLoader] Not initialized.");
            yield break;
        }

        if (IsBusy)
        {
            Debug.LogWarning("[SceneLoader] Busy.");
            yield break;
        }

        if (!IsSceneLoaded(sceneName))
        {
            Debug.LogWarning($"[SceneLoader] Scene is not loaded: {sceneName}");
            yield break;
        }

        var activeScene = UnitySceneManager.GetActiveScene();
        if (activeScene.name == sceneName && UnitySceneManager.sceneCount <= 1)
        {
            Debug.LogWarning($"[SceneLoader] Cannot unload the last active scene: {sceneName}");
            yield break;
        }

        IsBusy = true;

        AsyncOperation op = UnitySceneManager.UnloadSceneAsync(sceneName);
        if (op == null)
        {
            IsBusy = false;
            Debug.LogError($"[SceneLoader] Failed to start unload: {sceneName}");
            yield break;
        }

        yield return op;

        IsBusy = false;
    }

    private IEnumerator LoadInternalAsync(string sceneName, LoadSceneMode mode)
    {
        if (!IsInitialized)
        {
            Debug.LogError("[SceneLoader] Not initialized.");
            yield break;
        }

        if (IsBusy)
        {
            Debug.LogWarning("[SceneLoader] Busy.");
            yield break;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[SceneLoader] Scene cannot be loaded: {sceneName}");
            yield break;
        }

        if (mode == LoadSceneMode.Single && ActiveSceneName == sceneName)
        {
            yield break;
        }

        if (mode == LoadSceneMode.Additive && IsSceneLoaded(sceneName))
        {
            yield break;
        }

        IsBusy = true;

        AsyncOperation op = UnitySceneManager.LoadSceneAsync(sceneName, mode);
        if (op == null)
        {
            IsBusy = false;
            Debug.LogError($"[SceneLoader] Failed to start load: {sceneName}");
            yield break;
        }

        yield return op;

        IsBusy = false;
    }

    private bool IsSceneLoaded(string sceneName)
    {
        Scene scene = UnitySceneManager.GetSceneByName(sceneName);
        return scene.IsValid() && scene.isLoaded;
    }
}
