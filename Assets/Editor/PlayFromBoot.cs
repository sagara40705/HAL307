#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PlayFromBoot
{
    private const string BootScenePath = "Assets/Game/Scenes/Boot.unity";
    private const string TargetSceneKey = "PlayFromBoot.TargetScenePath";

    static PlayFromBoot()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state != PlayModeStateChange.ExitingEditMode)
            return;

        var activeScene = SceneManager.GetActiveScene();

        // Bootを開いた状態で再生したなら、遷移先は空にする
        var targetPath = activeScene.path == BootScenePath ? string.Empty : activeScene.path;
        SessionState.SetString(TargetSceneKey, targetPath);

        // Play開始時は必ずBootから始める
        EditorSceneManager.playModeStartScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(BootScenePath);
    }
}

#endif