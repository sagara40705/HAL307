#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
#if UNITY_6000_0_OR_NEWER
using UnityEditor.Build.Profile;
#endif
using UnityEngine;

public static class BuildSceneListSetup
{
    public readonly struct Config
    {
        public readonly string[] SceneSearchFolders;
        public readonly string[] ExcludedScenePathPrefixes;
        public readonly string[] PriorityScenePaths;
        public readonly bool SyncOverrideBuildProfiles;

        public Config(
            string[] sceneSearchFolders,
            string[] excludedScenePathPrefixes = null,
            string[] priorityScenePaths = null,
            bool syncOverrideBuildProfiles = true)
        {
            SceneSearchFolders = sceneSearchFolders ?? Array.Empty<string>();
            ExcludedScenePathPrefixes = excludedScenePathPrefixes ?? Array.Empty<string>();
            PriorityScenePaths = priorityScenePaths ?? Array.Empty<string>();
            SyncOverrideBuildProfiles = syncOverrideBuildProfiles;
        }
    }

    public readonly struct Result
    {
        public readonly bool HasDifference;
        public readonly bool Applied;
        public readonly int SceneCount;
        public readonly int SyncedBuildProfileCount;

        public Result(bool hasDifference, bool applied, int sceneCount, int syncedBuildProfileCount)
        {
            HasDifference = hasDifference;
            Applied = applied;
            SceneCount = sceneCount;
            SyncedBuildProfileCount = syncedBuildProfileCount;
        }
    }

    /// <summary>
    /// dryRun = true なら検証のみ
    /// dryRun = false なら必要時に適用
    /// </summary>
    public static Result ValidateOrApply(Config config, bool dryRun, StringBuilder report)
    {
        if (report == null)
        {
            report = new StringBuilder();
        }

        if (config.SceneSearchFolders == null || config.SceneSearchFolders.Length == 0)
        {
            report.AppendLine("[BuildSceneList] NG: SceneSearchFolders が空です。");
            return new Result(hasDifference: true, applied: false, sceneCount: 0, syncedBuildProfileCount: 0);
        }

        EditorBuildSettingsScene[] desiredScenes = CollectDesiredScenes(config);
        if (desiredScenes.Length == 0)
        {
            report.AppendLine("[BuildSceneList] NG: 対象 Scene が 1 件も見つかりませんでした。");
            return new Result(hasDifference: true, applied: false, sceneCount: 0, syncedBuildProfileCount: 0);
        }

        bool hasDifference = false;
        bool applied = false;
        int syncedBuildProfileCount = 0;

        EditorBuildSettingsScene[] currentGlobalScenes = GetCurrentGlobalSceneList();
        bool globalSame = AreSameSceneList(currentGlobalScenes, desiredScenes);

        if (globalSame)
        {
            report.AppendLine($"[BuildSceneList] Global Scene List: OK ({desiredScenes.Length} scenes)");
        }
        else
        {
            hasDifference = true;

            if (dryRun)
            {
                report.AppendLine("[BuildSceneList] Global Scene List: DIFF");
                AppendSceneList(report, "  Desired", desiredScenes);
            }
            else
            {
                SetGlobalSceneList(CloneSceneList(desiredScenes));
                applied = true;

                report.AppendLine($"[BuildSceneList] Global Scene List: APPLIED ({desiredScenes.Length} scenes)");
                AppendSceneList(report, "  Applied", desiredScenes);
            }
        }

#if UNITY_6000_0_OR_NEWER
        if (config.SyncOverrideBuildProfiles)
        {
            int changedProfiles = ValidateOrApplyOverrideBuildProfiles(desiredScenes, dryRun, report, out bool profileDiff);
            syncedBuildProfileCount = changedProfiles;

            if (profileDiff)
            {
                hasDifference = true;
            }

            if (!dryRun && changedProfiles > 0)
            {
                applied = true;
            }
        }
#endif

        if (!hasDifference)
        {
            report.AppendLine("[BuildSceneList] All scene settings already match.");
        }

        if (applied)
        {
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        return new Result(
            hasDifference: hasDifference,
            applied: applied,
            sceneCount: desiredScenes.Length,
            syncedBuildProfileCount: syncedBuildProfileCount);
    }

    public static EditorBuildSettingsScene[] CollectDesiredScenes(Config config)
    {
        var scenePaths = new List<string>();

        string[] guids = AssetDatabase.FindAssets("t:SceneAsset", config.SceneSearchFolders);
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (string.IsNullOrEmpty(path))
                continue;

            if (!path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
                continue;

            if (IsExcludedScenePath(path, config.ExcludedScenePathPrefixes))
                continue;

            scenePaths.Add(path);
        }

        scenePaths = scenePaths
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => GetScenePriority(path, config.PriorityScenePaths))
            .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return scenePaths
            .Select(path => new EditorBuildSettingsScene(path, true))
            .ToArray();
    }

    private static bool IsExcludedScenePath(string path, string[] excludedPrefixes)
    {
        if (excludedPrefixes == null || excludedPrefixes.Length == 0)
            return false;

        for (int i = 0; i < excludedPrefixes.Length; i++)
        {
            string prefix = excludedPrefixes[i];
            if (string.IsNullOrEmpty(prefix))
                continue;

            if (path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static int GetScenePriority(string path, string[] priorityScenePaths)
    {
        if (priorityScenePaths == null || priorityScenePaths.Length == 0)
            return int.MaxValue;

        for (int i = 0; i < priorityScenePaths.Length; i++)
        {
            if (string.Equals(path, priorityScenePaths[i], StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return int.MaxValue;
    }

    private static EditorBuildSettingsScene[] GetCurrentGlobalSceneList()
    {
#if UNITY_6000_0_OR_NEWER
        return EditorBuildSettings.globalScenes ?? Array.Empty<EditorBuildSettingsScene>();
#else
        return EditorBuildSettings.scenes ?? Array.Empty<EditorBuildSettingsScene>();
#endif
    }

    private static void SetGlobalSceneList(EditorBuildSettingsScene[] scenes)
    {
#if UNITY_6000_0_OR_NEWER
        EditorBuildSettings.globalScenes = scenes;
#else
        EditorBuildSettings.scenes = scenes;
#endif
    }

#if UNITY_6000_0_OR_NEWER
    private static int ValidateOrApplyOverrideBuildProfiles(
        EditorBuildSettingsScene[] desiredScenes,
        bool dryRun,
        StringBuilder report,
        out bool hasDifference)
    {
        hasDifference = false;
        int changedCount = 0;

        string[] guids = AssetDatabase.FindAssets("t:BuildProfile");
        if (guids == null || guids.Length == 0)
        {
            report.AppendLine("[BuildSceneList] Build Profiles: none");
            return 0;
        }

        int targetCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            BuildProfile profile = AssetDatabase.LoadAssetAtPath<BuildProfile>(path);
            if (profile == null)
                continue;

            if (!profile.overrideGlobalScenes)
                continue;

            targetCount++;

            bool same = AreSameSceneList(profile.scenes, desiredScenes);
            if (same)
            {
                report.AppendLine($"[BuildSceneList] BuildProfile Sync: OK ({profile.name})");
                continue;
            }

            hasDifference = true;

            if (dryRun)
            {
                report.AppendLine($"[BuildSceneList] BuildProfile Sync: DIFF ({profile.name})");
                continue;
            }

            profile.scenes = CloneSceneList(desiredScenes);
            EditorUtility.SetDirty(profile);
            changedCount++;

            report.AppendLine($"[BuildSceneList] BuildProfile Sync: APPLIED ({profile.name})");
        }

        if (targetCount == 0)
        {
            report.AppendLine("[BuildSceneList] Build Profiles: no profiles using Override Global Scene List");
        }

        return changedCount;
    }
#endif

    private static bool AreSameSceneList(EditorBuildSettingsScene[] a, EditorBuildSettingsScene[] b)
    {
        if (ReferenceEquals(a, b))
            return true;

        if (a == null || b == null)
            return false;

        if (a.Length != b.Length)
            return false;

        for (int i = 0; i < a.Length; i++)
        {
            if (!string.Equals(a[i].path, b[i].path, StringComparison.OrdinalIgnoreCase))
                return false;

            if (a[i].enabled != b[i].enabled)
                return false;
        }

        return true;
    }

    private static EditorBuildSettingsScene[] CloneSceneList(EditorBuildSettingsScene[] source)
    {
        if (source == null)
            return Array.Empty<EditorBuildSettingsScene>();

        var cloned = new EditorBuildSettingsScene[source.Length];
        for (int i = 0; i < source.Length; i++)
        {
            cloned[i] = new EditorBuildSettingsScene(source[i].path, source[i].enabled);
        }

        return cloned;
    }

    private static void AppendSceneList(StringBuilder report, string header, EditorBuildSettingsScene[] scenes)
    {
        report.AppendLine(header + ":");

        for (int i = 0; i < scenes.Length; i++)
        {
            report.AppendLine($"    [{i}] {scenes[i].path}");
        }
    }
}

#endif
