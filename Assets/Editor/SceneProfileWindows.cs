using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneProfileSettingsWindow : EditorWindow
{
    private const string MenuPath = "Tools/Profile/Settings";
    private const string SettingsAssetPath = "Assets/Game/Core/Profile/Resources/SceneProfileSettings.asset";
    private const int Priority = 240;

    private SceneProfileSettings _settings;
    private Vector2 _scroll;

    [MenuItem(MenuPath, false, Priority)]
    private static void OpenWindow()
    {
        SceneProfileSettingsWindow window = GetWindow<SceneProfileSettingsWindow>("Profile Settings");
        window.minSize = new Vector2(560f, 420f);
        window.Show();
    }

    private void OnEnable()
    {
        LoadOrCreateSettings();
        SyncSceneTargets();
    }

    private void OnGUI()
    {
        LoadOrCreateSettings();

        if (_settings == null)
        {
            EditorGUILayout.HelpBox("SceneProfileSettings could not be loaded or created.", MessageType.Error);
            return;
        }

        DrawToolbar();
        DrawSamplingSettings();
        DrawSceneList();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("Refresh Scenes", EditorStyles.toolbarButton, GUILayout.Width(105f)))
            {
                SyncSceneTargets();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Ping Asset", EditorStyles.toolbarButton, GUILayout.Width(75f)))
            {
                EditorGUIUtility.PingObject(_settings);
            }
        }
    }

    private void DrawSamplingSettings()
    {
        SerializedObject serializedObject = new SerializedObject(_settings);
        serializedObject.Update();

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Sampling", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(serializedObject.FindProperty("autoMeasure"), new GUIContent("Auto Measure"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("sampleIntervalSeconds"), new GUIContent("Interval Seconds"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("settleFrames"), new GUIContent("Settle Frames"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("sampleFrames"), new GUIContent("Sample Frames"));

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSceneList()
    {
        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Target Scenes", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Selected scenes are measured only while they are loaded. The runtime manager disables each scene's root GameObjects briefly and records the frame-time difference.", MessageType.Info);

        IReadOnlyDictionary<string, int> duplicateNameCounts = BuildSceneNameCounts(_settings.Targets);

        using (EditorGUILayout.ScrollViewScope scrollView = new EditorGUILayout.ScrollViewScope(_scroll))
        {
            _scroll = scrollView.scrollPosition;

            for (int i = 0; i < _settings.Targets.Count; i++)
            {
                SceneProfileTarget target = _settings.Targets[i];
                if (target == null)
                {
                    continue;
                }

                DrawTarget(target, duplicateNameCounts);
            }
        }
    }

    private void DrawTarget(SceneProfileTarget target, IReadOnlyDictionary<string, int> duplicateNameCounts)
    {
        bool isLoaded = IsSceneLoaded(target);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                bool enabled = EditorGUILayout.ToggleLeft(target.SceneName, target.IsEnabled, GUILayout.MinWidth(180f));
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_settings, "Change Scene Profile Target");
                    target.SetEnabled(enabled);
                    EditorUtility.SetDirty(_settings);
                }

                GUILayout.FlexibleSpace();

                string loadedText = isLoaded ? "Loaded" : "Not Loaded";
                GUILayout.Label(loadedText, EditorStyles.miniLabel, GUILayout.Width(75f));
            }

            EditorGUILayout.LabelField(target.ScenePath, EditorStyles.miniLabel);

            if (duplicateNameCounts.TryGetValue(target.SceneName, out int count) && count > 1)
            {
                EditorGUILayout.HelpBox("Another scene has the same name. Path matching is used first, but unique scene names are safer.", MessageType.Warning);
            }
        }
    }

    private void LoadOrCreateSettings()
    {
        if (_settings != null)
        {
            return;
        }

        _settings = AssetDatabase.LoadAssetAtPath<SceneProfileSettings>(SettingsAssetPath);
        if (_settings != null)
        {
            return;
        }

        string directory = Path.GetDirectoryName(SettingsAssetPath);
        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
            AssetDatabase.Refresh();
        }

        _settings = CreateInstance<SceneProfileSettings>();
        AssetDatabase.CreateAsset(_settings, SettingsAssetPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private void SyncSceneTargets()
    {
        if (_settings == null)
        {
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets" });
        string[] scenePaths = guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Undo.RecordObject(_settings, "Refresh Scene Profile Targets");

        for (int i = 0; i < scenePaths.Length; i++)
        {
            string path = scenePaths[i];
            string sceneName = Path.GetFileNameWithoutExtension(path);
            SceneProfileTarget existing = _settings.Targets.FirstOrDefault(
                target => string.Equals(target.ScenePath, path, StringComparison.OrdinalIgnoreCase));

            if (existing != null)
            {
                existing.UpdateScene(sceneName, path);
                continue;
            }

            _settings.Targets.Add(new SceneProfileTarget(sceneName, path, false));
        }

        _settings.Targets.Sort((a, b) => string.Compare(a.ScenePath, b.ScenePath, StringComparison.OrdinalIgnoreCase));
        EditorUtility.SetDirty(_settings);
        AssetDatabase.SaveAssets();
        Repaint();
    }

    private static IReadOnlyDictionary<string, int> BuildSceneNameCounts(IEnumerable<SceneProfileTarget> targets)
    {
        return targets
            .Where(target => target != null && !string.IsNullOrEmpty(target.SceneName))
            .GroupBy(target => target.SceneName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.OrdinalIgnoreCase);
    }

    private static bool IsSceneLoaded(SceneProfileTarget target)
    {
        if (!string.IsNullOrEmpty(target.ScenePath))
        {
            Scene sceneByPath = SceneManager.GetSceneByPath(target.ScenePath);
            if (sceneByPath.IsValid() && sceneByPath.isLoaded)
            {
                return true;
            }
        }

        Scene sceneByName = SceneManager.GetSceneByName(target.SceneName);
        return sceneByName.IsValid() && sceneByName.isLoaded;
    }
}

public sealed class SceneProfileResultWindow : EditorWindow
{
    private const string MenuPath = "Tools/Profile/Result";
    private const int Priority = 241;

    private Vector2 _scroll;

    [MenuItem(MenuPath, false, Priority)]
    private static void OpenWindow()
    {
        SceneProfileResultWindow window = GetWindow<SceneProfileResultWindow>("Profile Result");
        window.minSize = new Vector2(560f, 360f);
        window.Show();
    }

    private void OnEnable()
    {
        EditorApplication.update += RepaintWhilePlaying;
    }

    private void OnDisable()
    {
        EditorApplication.update -= RepaintWhilePlaying;
    }

    private void OnGUI()
    {
        DrawToolbar();

        SceneProfileReport report = SceneProfileManager.LastReport ?? SceneProfileManager.LoadLastReportFromPrefs();
        if (report == null)
        {
            EditorGUILayout.HelpBox("No scene profile result has been recorded yet.", MessageType.Info);
            return;
        }

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("Measured At", report.measuredAt);
        EditorGUILayout.LabelField("Sample Frames", report.sampleFrames.ToString());
        EditorGUILayout.LabelField("Settle Frames", report.settleFrames.ToString());

        EditorGUILayout.Space(8f);

        using (EditorGUILayout.ScrollViewScope scrollView = new EditorGUILayout.ScrollViewScope(_scroll))
        {
            _scroll = scrollView.scrollPosition;

            for (int i = 0; i < report.samples.Count; i++)
            {
                DrawSample(report.samples[i]);
            }
        }
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            using (new EditorGUI.DisabledScope(!CanRequestMeasure()))
            {
                if (GUILayout.Button("Measure Now", EditorStyles.toolbarButton, GUILayout.Width(95f)))
                {
                    SceneProfileManager.Instance.RequestMeasureOnce();
                }
            }

            if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(52f)))
            {
                SceneProfileManager.ClearLastReport();
            }

            GUILayout.FlexibleSpace();

            string status = GetRuntimeStatus();
            GUILayout.Label(status, EditorStyles.miniLabel);
        }
    }

    private static void DrawSample(SceneProfileSample sample)
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(sample.sceneName, EditorStyles.boldLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.LabelField(sample.state.ToString(), GUILayout.Width(95f));
            }

            EditorGUILayout.LabelField(sample.scenePath, EditorStyles.miniLabel);

            if (sample.state == SceneProfileSampleState.Measured)
            {
                EditorGUILayout.LabelField("Estimated Scene Cost", $"{sample.estimatedSceneMs:0.###} ms");
                EditorGUILayout.LabelField("Baseline", $"{sample.baselineMs:0.###} ms");
                EditorGUILayout.LabelField("Disabled", $"{sample.disabledMs:0.###} ms");
                EditorGUILayout.LabelField("Root Objects", sample.rootObjectCount.ToString());
            }
            else if (!string.IsNullOrEmpty(sample.note))
            {
                EditorGUILayout.HelpBox(sample.note, MessageType.None);
            }
        }
    }

    private static bool CanRequestMeasure()
    {
        return EditorApplication.isPlaying &&
               SceneProfileManager.HasInstance &&
               SceneProfileManager.Instance.IsInitialized &&
               !SceneProfileManager.Instance.IsMeasuring;
    }

    private static string GetRuntimeStatus()
    {
        if (!EditorApplication.isPlaying)
        {
            return "Edit Mode";
        }

        if (!SceneProfileManager.HasInstance)
        {
            return "Manager Not Found";
        }

        return SceneProfileManager.Instance.IsMeasuring ? "Measuring" : "Ready";
    }

    private void RepaintWhilePlaying()
    {
        if (EditorApplication.isPlaying)
        {
            Repaint();
        }
    }
}

