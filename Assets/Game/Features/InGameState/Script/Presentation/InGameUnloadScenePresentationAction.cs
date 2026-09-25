using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[CreateAssetMenu(
    fileName = "InGameUnloadScenePresentationAction",
    menuName = "Game/InGame/Presentation/Unload Scene")]
public sealed class InGameUnloadScenePresentationAction : InGamePresentationAction
{
    [SerializeField] private string sceneName;

    public override IEnumerator Play(InGameFlowContext context)
    {
        Scene scene = SceneLookup.Find(sceneName);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning($"[InGameUnloadScenePresentationAction] Unload skipped. Scene is not loaded. Scene='{sceneName}'");
            yield break;
        }

        SceneLoader sceneLoader = SceneLoader.Instance;
        if (sceneLoader != null && sceneLoader.IsInitialized)
        {
            yield return sceneLoader.UnloadAsync(sceneName);
            yield break;
        }

        AsyncOperation operation = SceneManager.UnloadSceneAsync(scene);
        if (operation == null)
        {
            Debug.LogError($"[InGameUnloadScenePresentationAction] Unload failed. Scene='{sceneName}'");
            yield break;
        }

        yield return operation;
    }

}
