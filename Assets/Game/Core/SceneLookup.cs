using System;
using UnityEngine.SceneManagement;

// シーン名・アセットパスの解決をゲーム内で統一する。
public static class SceneLookup
{
    public static string NormalizeName(string scenePathOrName)
    {
        if (string.IsNullOrWhiteSpace(scenePathOrName))
        {
            return string.Empty;
        }

        string normalized = scenePathOrName.Trim().Replace('\\', '/');
        int slashIndex = normalized.LastIndexOf('/');
        string name = normalized.Substring(slashIndex + 1);
        const string extension = ".unity";
        return name.EndsWith(extension, StringComparison.OrdinalIgnoreCase)
            ? name.Substring(0, name.Length - extension.Length)
            : name;
    }

    public static Scene Find(string scenePathOrName)
    {
        if (string.IsNullOrWhiteSpace(scenePathOrName))
        {
            return default;
        }

        string path = scenePathOrName.Trim().Replace('\\', '/');
        Scene scene = SceneManager.GetSceneByPath(path);
        return scene.IsValid() ? scene : SceneManager.GetSceneByName(NormalizeName(path));
    }

    public static bool Matches(Scene scene, string scenePathOrName)
    {
        return scene.IsValid() && !string.IsNullOrWhiteSpace(scenePathOrName) &&
               scene == Find(scenePathOrName);
    }
}
