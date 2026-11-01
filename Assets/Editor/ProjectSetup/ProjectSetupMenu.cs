#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

public static class ProjectSetupMenu
{
    // ==============================
    // プロジェクト標準
    // ==============================

    private static readonly InputHandlingSetup.Mode DesiredInputHandling = InputHandlingSetup.Mode.NewInputSystem;

    private static readonly string[] SceneSearchFolders =
    {
        "Assets/Game",
        "Assets/_Shared",
    };

    private static readonly string[] PriorityScenePaths =
    {
        "Assets/Game/Scenes/Boot.unity",
        "Assets/Game/Scenes/Title.unity",
        "Assets/_Shared/Prototype/Scenes/Prototype.unity",
    };

    // ==============================
    // メニュー
    // ==============================

    [MenuItem("Tools/Project Setup/Validate Standard Settings", priority = 0)]
    private static void ValidateStandardSettings()
    {
        ProjectSetupRunner.Result result = ProjectSetupRunner.ValidateAll(CreateConfig(
            showCompletionDialog: false,
            showRestartDialog: false));

        string message = result.HasDifference
            ? "標準設定との差分があります。\n詳細は Console を確認してください。"
            : "差分はありません。すでに標準設定と一致しています。";

        EditorUtility.DisplayDialog("Project Setup", message, "OK");
    }

    [MenuItem("Tools/Project Setup/Apply Standard Settings", priority = 1)]
    private static void ApplyStandardSettings()
    {
        ProjectSetupRunner.ApplyAll(CreateConfig(
            showCompletionDialog: true,
            showRestartDialog: true));
    }

    // ==============================
    // 設定
    // ==============================

    private static ProjectSetupRunner.Config CreateConfig(
        bool showCompletionDialog,
        bool showRestartDialog)
    {
        return new ProjectSetupRunner.Config(
            runInputHandling: true,
            runBuildSceneList: true,
            desiredInputHandling: DesiredInputHandling,
            buildSceneListConfig: new BuildSceneListSetup.Config(
                sceneSearchFolders: SceneSearchFolders,
                priorityScenePaths: PriorityScenePaths,
                syncOverrideBuildProfiles: true
            ),
            logReportToConsole: true,
            showCompletionDialog: showCompletionDialog,
            showRestartDialog: showRestartDialog
        );
    }
}

#endif
