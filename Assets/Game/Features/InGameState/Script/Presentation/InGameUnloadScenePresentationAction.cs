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
        Scene scene = FindScene(sceneName);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning($"[InGameUnloadScenePresentationAction] Unload skipped. Scene is not loaded. Scene='{sceneName}'");
            yield break;
        }

        SceneLoader sceneLoader = SceneLoader.Instance;
        if (sceneLoader != null && sceneLoader.IsInitialized)
        {
            yield return sceneLoader.UnloadAsync(scene.name);
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

    private static Scene FindScene(string scenePathOrName)
    {
        if (string.IsNullOrWhiteSpace(scenePathOrName))
        {
            return default;
        }

        string normalizedPath = scenePathOrName.Replace('\\', '/');
        Scene scene = SceneManager.GetSceneByPath(normalizedPath);
        if (scene.IsValid())
        {
            return scene;
        }

        int slashIndex = normalizedPath.LastIndexOf('/');
        string name = slashIndex >= 0 ? normalizedPath.Substring(slashIndex + 1) : normalizedPath;
        if (name.EndsWith(".unity", System.StringComparison.OrdinalIgnoreCase))
        {
            name = name.Substring(0, name.Length - ".unity".Length);
        }

        return SceneManager.GetSceneByName(name);
    }
}
