#if UNITY_EDITOR

using System.Text;
using UnityEditor;
using UnityEngine;

public static class ProjectSetupRunner
{
    public readonly struct Config
    {
        public readonly bool RunInputHandling;
        public readonly bool RunBuildSceneList;

        public readonly InputHandlingSetup.Mode DesiredInputHandling;
        public readonly BuildSceneListSetup.Config BuildSceneListConfig;

        public readonly bool LogReportToConsole;
        public readonly bool ShowCompletionDialog;
        public readonly bool ShowRestartDialog;

        public Config(
            bool runInputHandling,
            bool runBuildSceneList,
            InputHandlingSetup.Mode desiredInputHandling,
            BuildSceneListSetup.Config buildSceneListConfig,
            bool logReportToConsole = true,
            bool showCompletionDialog = false,
            bool showRestartDialog = true)
        {
            RunInputHandling = runInputHandling;
            RunBuildSceneList = runBuildSceneList;
            DesiredInputHandling = desiredInputHandling;
            BuildSceneListConfig = buildSceneListConfig;
            LogReportToConsole = logReportToConsole;
            ShowCompletionDialog = showCompletionDialog;
            ShowRestartDialog = showRestartDialog;
        }
    }

    public readonly struct Result
    {
        public readonly bool HasDifference;
        public readonly bool AppliedAny;
        public readonly bool RequiresEditorRestart;
        public readonly int SceneCount;
        public readonly int SyncedBuildProfileCount;
        public readonly string Report;

        public Result(
            bool hasDifference,
            bool appliedAny,
            bool requiresEditorRestart,
            int sceneCount,
            int syncedBuildProfileCount,
            string report)
        {
            HasDifference = hasDifference;
            AppliedAny = appliedAny;
            RequiresEditorRestart = requiresEditorRestart;
            SceneCount = sceneCount;
            SyncedBuildProfileCount = syncedBuildProfileCount;
            Report = report;
        }
    }

    public static Result ValidateAll(Config config)
    {
        return Execute(config, dryRun: true);
    }

    public static Result ApplyAll(Config config)
    {
        return Execute(config, dryRun: false);
    }

    private static Result Execute(Config config, bool dryRun)
    {
        var report = new StringBuilder();

        report.AppendLine("=== Project Setup ===");
        report.AppendLine(dryRun ? "[Mode] Validate" : "[Mode] Apply");

        bool hasDifference = false;
        bool appliedAny = false;
        bool requiresEditorRestart = false;

        int sceneCount = 0;
        int syncedBuildProfileCount = 0;

        if (config.RunInputHandling)
        {
            var inputResult = InputHandlingSetup.ValidateOrApply(
                config.DesiredInputHandling,
                dryRun,
                report);

            hasDifference |= inputResult.HasDifference;
            appliedAny |= inputResult.Applied;
            requiresEditorRestart |= inputResult.RequiresEditorRestart;
        }
        else
        {
            report.AppendLine("[InputHandling] SKIPPED");
        }

        if (config.RunBuildSceneList)
        {
            var buildSceneResult = BuildSceneListSetup.ValidateOrApply(
                config.BuildSceneListConfig,
                dryRun,
                report);

            hasDifference |= buildSceneResult.HasDifference;
            appliedAny |= buildSceneResult.Applied;
            sceneCount = buildSceneResult.SceneCount;
            syncedBuildProfileCount = buildSceneResult.SyncedBuildProfileCount;
        }
        else
        {
            report.AppendLine("[BuildSceneList] SKIPPED");
        }

        if (!hasDifference)
        {
            report.AppendLine("All selected setup tasks already match the standard.");
        }

        string reportText = report.ToString();

        if (config.LogReportToConsole)
        {
            Debug.Log(reportText);
        }

        if (!dryRun)
        {
            if (config.ShowCompletionDialog)
            {
                string message = appliedAny
                    ? "Project Setup の適用が完了しました。"
                    : "変更はありませんでした。";

                if (sceneCount > 0)
                {
                    message += $"\n\nScene Count: {sceneCount}";
                }

                if (syncedBuildProfileCount > 0)
                {
                    message += $"\nSynced Build Profiles: {syncedBuildProfileCount}";
                }

                EditorUtility.DisplayDialog(
                    "Project Setup",
                    message,
                    "OK");
            }

            if (requiresEditorRestart && config.ShowRestartDialog)
            {
                EditorUtility.DisplayDialog(
                    "Project Setup",
                    "Active Input Handling を変更しました。\nEditor の再起動が必要です。",
                    "OK");
            }
        }

        return new Result(
            hasDifference: hasDifference,
            appliedAny: appliedAny,
            requiresEditorRestart: requiresEditorRestart,
            sceneCount: sceneCount,
            syncedBuildProfileCount: syncedBuildProfileCount,
            report: reportText);
    }
}

#endif
