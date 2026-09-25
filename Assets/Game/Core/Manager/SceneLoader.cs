using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnitySceneManager = UnityEngine.SceneManagement.SceneManager;

public sealed class SceneLoader : AppManagerBase<SceneLoader>
{
    public bool IsBusy { get; private set; }

    public string ActiveSceneName => UnitySceneManager.GetActiveScene().name;

    public static bool Load(string sceneName)
    {
        return StartLoad(sceneName, LoadSceneMode.Single);
    }

    public static bool LoadAdditive(string sceneName)
    {
        return StartLoad(sceneName, LoadSceneMode.Additive);
    }

    public static bool Unload(string sceneName)
    {
        if (!TryGetReadyInstance("Unload", sceneName, out SceneLoader sceneLoader))
        {
            return false;
        }

        sceneLoader.StartCoroutine(sceneLoader.UnloadAsync(sceneName));
        return true;
    }

    private static bool StartLoad(string sceneName, LoadSceneMode mode)
    {
        SceneLoader sceneLoader = Instance;
        if (sceneLoader == null)
        {
            return LoadWithoutSceneLoader(sceneName, mode);
        }

        if (!sceneLoader.IsInitialized)
        {
            Debug.LogError($"[SceneLoader] Load failed. Reason=Not initialized. Scene='{sceneName}', Mode={mode}");
            return false;
        }

        sceneLoader.StartCoroutine(sceneLoader.LoadCoroutine(sceneName, mode));
        return true;
    }

    protected override IEnumerator OnInitialize()
    {
        IsBusy = false;
        UnitySceneManager.sceneUnloaded += OnSceneUnloaded;
        yield break;
    }

    public IEnumerator LoadAsync(string sceneName)
    {
        yield return RunManagedOperation(LoadCoroutine(sceneName, LoadSceneMode.Single));
    }

    public IEnumerator LoadAdditiveAsync(string sceneName)
    {
        yield return RunManagedOperation(LoadCoroutine(sceneName, LoadSceneMode.Additive));
    }

    public IEnumerator LoadAdditiveAsync(string sceneName, int priority)
    {
        yield return RunManagedOperation(LoadCoroutine(sceneName, LoadSceneMode.Additive, priority));
    }

    public IEnumerator UnloadAsync(string sceneName)
    {
        yield return RunManagedOperation(UnloadCoroutine(sceneName));
    }

    private IEnumerator UnloadCoroutine(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[SceneLoader] Unload failed. Reason=Scene name is empty.");
            yield break;
        }

        if (!IsInitialized)
        {
            Debug.LogError($"[SceneLoader] Unload failed. Reason=Not initialized. Scene='{sceneName}'");
            yield break;
        }

        yield return WaitUntilIdle("Unload", sceneName, null);

        if (!IsSceneLoaded(sceneName))
        {
            Debug.LogWarning($"[SceneLoader] Unload skipped. Reason=Scene is not loaded. Scene='{sceneName}', LoadedScenes=[{GetLoadedSceneNames()}]");
            yield break;
        }

        var activeScene = UnitySceneManager.GetActiveScene();
        if (activeScene.name == sceneName && UnitySceneManager.sceneCount <= 1)
        {
            Debug.LogWarning($"[SceneLoader] Unload skipped. Reason=Cannot unload the last active scene. Scene='{sceneName}'");
            yield break;
        }

        IsBusy = true;

        AsyncOperation op = UnitySceneManager.UnloadSceneAsync(sceneName);
        if (op == null)
        {
            IsBusy = false;
            Debug.LogError($"[SceneLoader] Unload failed. Reason=UnloadSceneAsync returned null. Scene='{sceneName}', LoadedScenes=[{GetLoadedSceneNames()}]");
            yield break;
        }

        yield return op;

        if (IsSceneLoaded(sceneName))
        {
            Debug.LogError($"[SceneLoader] Unload failed. Reason=Scene is still loaded after async operation. Scene='{sceneName}', LoadedScenes=[{GetLoadedSceneNames()}]");
        }

        IsBusy = false;
    }

    private IEnumerator RunManagedOperation(IEnumerator operation)
    {
        bool isDone = false;
        StartCoroutine(RunOperation(operation, () => isDone = true));

        while (!isDone)
        {
            yield return null;
        }
    }

    private IEnumerator RunOperation(IEnumerator operation, Action onCompleted)
    {
        yield return operation;
        onCompleted?.Invoke();
    }

    private IEnumerator LoadCoroutine(string sceneName, LoadSceneMode mode, int priority = 0)
    {
        Debug.Log(
            $"[SceneLoadTrace] " +
            $"sceneName={sceneName}, " +
            $"mode={mode}, " +
            $"activeScene={UnitySceneManager.GetActiveScene().name}, " +
            $"loadedScenes=[{GetLoadedSceneNames()}]"
        );

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError($"[SceneLoader] Load failed. Reason=Scene name is empty. Mode={mode}");
            yield break;
        }

        if (!IsInitialized)
        {
            Debug.LogError($"[SceneLoader] Load failed. Reason=Not initialized. Scene='{sceneName}', Mode={mode}");
            yield break;
        }

        yield return WaitUntilIdle("Load", sceneName, mode);

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[SceneLoader] Load failed. Reason=Scene cannot be loaded by Application.CanStreamedLevelBeLoaded. Scene='{sceneName}', Mode={mode}. Check Build Settings scene list and scene name.");
            yield break;
        }

        if (mode == LoadSceneMode.Single && ActiveSceneName == sceneName)
        {
            Debug.LogWarning($"[SceneLoader] Load skipped. Reason=Scene is already active. Scene='{sceneName}', Mode={mode}");
            yield break;
        }

        if (mode == LoadSceneMode.Additive && IsSceneLoaded(sceneName))
        {
            Debug.LogWarning($"[SceneLoader] Load skipped. Reason=Scene is already loaded. Scene='{sceneName}', Mode={mode}, LoadedScenes=[{GetLoadedSceneNames()}]");
            yield break;
        }

        IsBusy = true;

        AsyncOperation op = UnitySceneManager.LoadSceneAsync(sceneName, mode);
        if (op == null)
        {
            IsBusy = false;
            Debug.LogError($"[SceneLoader] Load failed. Reason=LoadSceneAsync returned null. Scene='{sceneName}', Mode={mode}");
            yield break;
        }

        op.priority = priority;

        yield return op;

        if (!IsSceneLoaded(sceneName))
        {
            Debug.LogError($"[SceneLoader] Load failed. Reason=Scene is not loaded after async operation. Scene='{sceneName}', Mode={mode}, LoadedScenes=[{GetLoadedSceneNames()}]");
            IsBusy = false;
            yield break;
        }

        if (mode == LoadSceneMode.Additive)
        {
            Scene loadedScene = GetLoadedScene(sceneName);
            foreach (var root in loadedScene.GetRootGameObjects())
            {
                var disabler = root.GetComponentInChildren<AdditiveSceneObjectDisabler>(true);
                if (disabler != null)
                {
                    disabler.DisableTargets();
                    Debug.Log($"Disabled Target objects in {sceneName}");
                    break; // 最初に見つけた1つだけ
                }
            }
        }

        IsBusy = false;
    }

    public bool IsSceneLoaded(string sceneName)
    {
        Scene scene = GetLoadedScene(sceneName);
        return scene.IsValid() && scene.isLoaded;
    }

    private static Scene GetLoadedScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return default;
        }

        string normalizedScenePath = sceneName.Replace('\\', '/');
        Scene scene = UnitySceneManager.GetSceneByPath(normalizedScenePath);
        if (scene.IsValid())
        {
            return scene;
        }

        return UnitySceneManager.GetSceneByName(GetSceneNameWithoutPath(normalizedScenePath));
    }

    private static string GetSceneNameWithoutPath(string sceneName)
    {
        int slashIndex = sceneName.LastIndexOf('/');
        string name = slashIndex >= 0 ? sceneName.Substring(slashIndex + 1) : sceneName;

        const string unityExtension = ".unity";
        if (name.EndsWith(unityExtension, StringComparison.OrdinalIgnoreCase))
        {
            name = name.Substring(0, name.Length - unityExtension.Length);
        }

        return name;
    }

    private IEnumerator WaitUntilIdle(string actionName, string sceneName, LoadSceneMode? mode)
    {
        if (!IsBusy)
        {
            yield break;
        }

        string modeText = mode.HasValue ? $", Mode={mode.Value}" : string.Empty;
        Debug.Log($"[SceneLoader] {actionName} waiting. Reason=Loader is busy. Scene='{sceneName}'{modeText}, ActiveScene='{ActiveSceneName}'");

        while (IsBusy)
        {
            yield return null;
        }
    }

    private string GetLoadedSceneNames()
    {
        if (UnitySceneManager.sceneCount <= 0)
        {
            return string.Empty;
        }

        string[] sceneNames = new string[UnitySceneManager.sceneCount];
        for (int i = 0; i < UnitySceneManager.sceneCount; i++)
        {
            sceneNames[i] = UnitySceneManager.GetSceneAt(i).name;
        }

        return string.Join(", ", sceneNames);
    }

    private void OnSceneUnloaded(Scene scene)
    {
        Debug.Log($"[SceneUnloadTrace] scene={scene.name}");
    }

    private static bool TryGetReadyInstance(string actionName, string sceneName, out SceneLoader sceneLoader)
    {
        sceneLoader = Instance;
        if (sceneLoader == null)
        {
            Debug.LogError($"[SceneLoader] {actionName} failed. Reason=SceneLoader instance is not available. Scene='{sceneName}'");
            return false;
        }

        if (!sceneLoader.IsInitialized)
        {
            Debug.LogError($"[SceneLoader] {actionName} failed. Reason=SceneLoader is not initialized. Scene='{sceneName}'");
            return false;
        }

        return true;
    }

    private static bool LoadWithoutSceneLoader(string sceneName, LoadSceneMode mode)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError($"[SceneLoader] Load failed. Reason=Scene name is empty. Mode={mode}");
            return false;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[SceneLoader] Load failed. Reason=Scene cannot be loaded by Application.CanStreamedLevelBeLoaded. Scene='{sceneName}', Mode={mode}. Check Build Settings scene list and scene name.");
            return false;
        }

        Scene activeScene = UnitySceneManager.GetActiveScene();
        if (mode == LoadSceneMode.Single && activeScene.name == sceneName)
        {
            Debug.LogWarning($"[SceneLoader] Load skipped. Reason=Scene is already active. Scene='{sceneName}', Mode={mode}");
            return false;
        }

        Scene scene = UnitySceneManager.GetSceneByName(sceneName);
        if (mode == LoadSceneMode.Additive && scene.IsValid() && scene.isLoaded)
        {
            Debug.LogWarning($"[SceneLoader] Load skipped. Reason=Scene is already loaded. Scene='{sceneName}', Mode={mode}");
            return false;
        }

        Debug.LogWarning($"[SceneLoader] SceneLoader instance is not available. Falling back to Unity SceneManager. Scene='{sceneName}', Mode={mode}");

        AsyncOperation op = UnitySceneManager.LoadSceneAsync(sceneName, mode);
        if (op == null)
        {
            Debug.LogError($"[SceneLoader] Load failed. Reason=LoadSceneAsync returned null. Scene='{sceneName}', Mode={mode}");
            return false;
        }

        return true;
    }
}



