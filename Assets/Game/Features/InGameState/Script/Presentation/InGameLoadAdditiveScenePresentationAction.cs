using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

[CreateAssetMenu(fileName = "InGameLoadAdditiveScenePresentationAction", menuName = "Game/InGame/Presentation/Load Additive Scene")]
public sealed class InGameLoadAdditiveScenePresentationAction : InGamePresentationAction
{
    [SerializeField] private string sceneName;

    public override IEnumerator Play(InGameFlowContext context)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning("[InGameLoadAdditiveScenePresentationAction] Load skipped. Reason=Scene name is empty.");
            yield break;
        }

        if (context != null && context.IsScenePreloadTracked(sceneName))
        {
            yield return context.ActivatePreloadedScene(sceneName);
            Scene preloadedScene = GetLoadedScene();
            if (preloadedScene.IsValid() && preloadedScene.isLoaded)
            {
                yield break;
            }
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            yield return LoadWithoutSceneLoader();
            yield break;
        }

        SceneLoader sceneLoader = SceneLoader.Instance;
        if (sceneLoader != null && sceneLoader.IsInitialized)
        {
            yield return sceneLoader.LoadAdditiveAsync(sceneName);
            LogIfSceneWasNotLoaded();
            yield break;
        }

        yield return LoadWithoutSceneLoader();
    }

    private IEnumerator LoadWithoutSceneLoader()
    {
        Scene loadedScene = GetLoadedScene();
        if (loadedScene.IsValid() && loadedScene.isLoaded)
        {
            Debug.LogWarning($"[InGameLoadAdditiveScenePresentationAction] Load skipped. Reason=Scene is already loaded. Scene='{sceneName}'");
            yield break;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
#if UNITY_EDITOR
            AsyncOperation editorOperation = EditorSceneManager.LoadSceneAsyncInPlayMode(
                sceneName,
                new LoadSceneParameters(LoadSceneMode.Additive));
            if (editorOperation == null)
            {
                Debug.LogError($"[InGameLoadAdditiveScenePresentationAction] Editor load failed. Scene='{sceneName}'");
                yield break;
            }

            yield return editorOperation;
            LogIfSceneWasNotLoaded();
            yield break;
#else
            Debug.LogError($"[InGameLoadAdditiveScenePresentationAction] Load failed. Reason=Scene cannot be loaded. Scene='{sceneName}'. Check Build Settings scene list and scene name.");
            yield break;
#endif
        }

        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        if (operation == null)
        {
            Debug.LogError($"[InGameLoadAdditiveScenePresentationAction] Load failed. Reason=LoadSceneAsync returned null. Scene='{sceneName}'");
            yield break;
        }

        yield return operation;
        LogIfSceneWasNotLoaded();
    }

    private void LogIfSceneWasNotLoaded()
    {
        Scene loadedScene = GetLoadedScene();
        if (loadedScene.IsValid() && loadedScene.isLoaded)
        {
            return;
        }

        Debug.LogError($"[InGameLoadAdditiveScenePresentationAction] Load failed. Reason=Scene is not loaded after operation. Scene='{sceneName}'");
    }

    private Scene GetLoadedScene()
    {
        return SceneLookup.Find(sceneName);
    }
}
