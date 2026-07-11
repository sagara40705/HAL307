using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        if (loadedScene.IsValid() && loadedScene.isLoaded)
        {
            Debug.LogWarning($"[InGameLoadAdditiveScenePresentationAction] Load skipped. Reason=Scene is already loaded. Scene='{sceneName}'");
            yield break;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[InGameLoadAdditiveScenePresentationAction] Load failed. Reason=Scene cannot be loaded. Scene='{sceneName}'. Check Build Settings scene list and scene name.");
            yield break;
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
        Scene loadedScene = SceneManager.GetSceneByName(sceneName);
        if (loadedScene.IsValid() && loadedScene.isLoaded)
        {
            return;
        }

        Debug.LogError($"[InGameLoadAdditiveScenePresentationAction] Load failed. Reason=Scene is not loaded after operation. Scene='{sceneName}'");
    }
}
