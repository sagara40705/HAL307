using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

[CreateAssetMenu(
    fileName = "InGamePlaySceneTimelinePresentationAction",
    menuName = "Game/InGame/Presentation/Play Scene Timeline")]
public sealed class InGamePlaySceneTimelinePresentationAction : InGamePresentationAction
{
    [SerializeField] private string sceneName;
    [SerializeField, Min(0f)] private double startTime;

    public override IEnumerator Play(InGameFlowContext context)
    {
        Scene scene = FindScene(sceneName);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning($"[InGamePlaySceneTimelinePresentationAction] Play skipped. Scene is not loaded. Scene='{sceneName}'");
            yield break;
        }

        PlayableDirector director = FindDirector(scene);
        if (director == null)
        {
            Debug.LogWarning($"[InGamePlaySceneTimelinePresentationAction] Play skipped. PlayableDirector was not found. Scene='{sceneName}'");
            yield break;
        }

        director.time = startTime;
        director.Evaluate();
        director.Play();
    }

    private static PlayableDirector FindDirector(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            PlayableDirector director = root.GetComponentInChildren<PlayableDirector>(true);
            if (director != null)
            {
                return director;
            }
        }

        return null;
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
