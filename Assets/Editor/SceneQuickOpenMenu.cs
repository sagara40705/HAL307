using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class SceneQuickOpenWindow : EditorWindow
{
    private static readonly string[] SearchFolders =
    {
        "Assets/Game/Scenes",
        "Assets/_Sandbox",
        "Assets/_Shared"
    };

    private Vector2 _scroll;
    private string _searchText = string.Empty;
    private SceneEntry[] _scenes = Array.Empty<SceneEntry>();

    [MenuItem("Tools/Scenes/Open Scene...", priority = 200)]
    private static void OpenWindow()
    {
        SceneQuickOpenWindow window = GetWindow<SceneQuickOpenWindow>(
            utility: true,
            title: "Open Scene",
            focus: true);

        window.minSize = new Vector2(520f, 400f);
        window.RefreshScenes();
        window.Show();
    }

    private void OnEnable()
    {
        RefreshScenes();
    }

    private void OnFocus()
    {
        RefreshScenes();
    }

    private void OnGUI()
    {
        DrawToolbar();
        DrawSceneList();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("Search", GUILayout.Width(45));

            string newSearchText = GUILayout.TextField(
                _searchText,
                EditorStyles.toolbarTextField,
                GUILayout.ExpandWidth(true));

            if (!string.Equals(newSearchText, _searchText, StringComparison.Ordinal))
            {
                _searchText = newSearchText;
            }

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                RefreshScenes();
            }
        }
    }

    private void DrawSceneList()
    {
        string currentScenePath = SceneManager.GetActiveScene().path;
        SceneEntry[] filteredScenes = SceneQuickOpenUtility.FilterScenes(_scenes, _searchText);

        if (filteredScenes.Length == 0)
        {
            EditorGUILayout.HelpBox("No scenes found.", MessageType.Info);
            return;
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        foreach (SceneEntry scene in filteredScenes)
        {
            bool isCurrent = string.Equals(
                scene.Path,
                currentScenePath,
                StringComparison.OrdinalIgnoreCase);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.enabled = !isCurrent;

                    if (GUILayout.Button(scene.Label, GUILayout.Height(24)))
                    {
                        SceneQuickOpenUtility.OpenScene(scene.Path);
                        Close();
                    }

                    GUI.enabled = true;

                    if (isCurrent)
                    {
                        GUILayout.Label("Current", GUILayout.Width(60));
                    }
                }

                EditorGUILayout.LabelField(scene.Path, EditorStyles.miniLabel);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void RefreshScenes()
    {
        string[] guids = AssetDatabase.FindAssets("t:Scene", SearchFolders);

        _scenes = guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(SceneQuickOpenUtility.IsValidScenePath)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => new SceneEntry(path, SceneQuickOpenUtility.BuildLabel(path)))
            .ToArray();

        Repaint();
    }
}

public sealed class GamePhaseSceneQuickOpenWindow : EditorWindow
{
    private Vector2 _scroll;
    private string _searchText = string.Empty;
    private string _loadError = string.Empty;
    private GamePhaseSceneEntry[] _scenes = Array.Empty<GamePhaseSceneEntry>();

    [MenuItem("Tools/Scenes/Open Game Phase Scene...", priority = 201)]
    private static void OpenWindow()
    {
        GamePhaseSceneQuickOpenWindow window = GetWindow<GamePhaseSceneQuickOpenWindow>(
            utility: true,
            title: "Open Game Phase Scene",
            focus: true);

        window.minSize = new Vector2(560f, 400f);
        window.RefreshScenes();
        window.Show();
    }

    private void OnEnable()
    {
        RefreshScenes();
    }

    private void OnFocus()
    {
        RefreshScenes();
    }

    private void OnGUI()
    {
        DrawToolbar();
        DrawSceneList();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("Search", GUILayout.Width(45));

            string newSearchText = GUILayout.TextField(
                _searchText,
                EditorStyles.toolbarTextField,
                GUILayout.ExpandWidth(true));

            if (!string.Equals(newSearchText, _searchText, StringComparison.Ordinal))
            {
                _searchText = newSearchText;
            }

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70)))
            {
                RefreshScenes();
            }
        }
    }

    private void DrawSceneList()
    {
        if (!string.IsNullOrEmpty(_loadError))
        {
            EditorGUILayout.HelpBox(_loadError, MessageType.Warning);
        }

        string currentScenePath = SceneManager.GetActiveScene().path;
        GamePhaseSceneEntry[] filteredScenes = FilterScenes(_scenes, _searchText);

        if (filteredScenes.Length == 0)
        {
            EditorGUILayout.HelpBox("No GamePhase scenes found.", MessageType.Info);
            return;
        }

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        foreach (GamePhaseSceneEntry scene in filteredScenes)
        {
            bool isCurrent = scene.IsValid &&
                             string.Equals(
                                 scene.Path,
                                 currentScenePath,
                                 StringComparison.OrdinalIgnoreCase);

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.enabled = scene.IsValid && !isCurrent;

                    if (GUILayout.Button(scene.Label, GUILayout.Height(24)))
                    {
                        SceneQuickOpenUtility.OpenScene(scene.Path);
                        Close();
                    }

                    GUI.enabled = true;

                    if (isCurrent)
                    {
                        GUILayout.Label("Current", GUILayout.Width(60));
                    }
                    else if (!scene.IsValid)
                    {
                        GUILayout.Label("Missing", GUILayout.Width(60));
                    }
                }

                string pathText = scene.IsValid
                    ? scene.Path
                    : $"SceneName='{scene.SceneName}' is not found.";

                EditorGUILayout.LabelField(pathText, EditorStyles.miniLabel);
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void RefreshScenes()
    {
        _loadError = string.Empty;

        GamePhaseSceneSettings settings = GamePhaseSceneSettings.Load();
        if (settings == null)
        {
            _loadError = $"GamePhaseSceneSettings was not found in Resources. Expected Resources name='{GamePhaseSceneSettings.ResourcesName}'.";
            _scenes = Array.Empty<GamePhaseSceneEntry>();
            Repaint();
            return;
        }

        _scenes = settings.PhaseScenes
            .Where(binding => binding != null && binding.Phase != GamePhase.None)
            .Select(CreateEntry)
            .OrderBy(entry => entry.Phase)
            .ThenBy(entry => entry.SceneName, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Repaint();
    }

    private static GamePhaseSceneEntry CreateEntry(GamePhaseSceneBinding binding)
    {
        string sceneName = binding.SceneName;
        string scenePath = ResolveScenePath(sceneName);
        bool isValid = !string.IsNullOrWhiteSpace(scenePath);
        string label = isValid
            ? $"{binding.Phase} / {SceneQuickOpenUtility.BuildLabel(scenePath)}"
            : $"{binding.Phase} / {sceneName}";

        return new GamePhaseSceneEntry(binding.Phase, sceneName, scenePath, label, isValid);
    }

    private static string ResolveScenePath(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return string.Empty;
        }

        string normalizedSceneName = sceneName.Replace('\\', '/').Trim();
        if (normalizedSceneName.EndsWith(".unity", StringComparison.OrdinalIgnoreCase) &&
            AssetDatabase.LoadAssetAtPath<SceneAsset>(normalizedSceneName) != null)
        {
            return normalizedSceneName;
        }

        string sceneNameWithoutExtension = Path.GetFileNameWithoutExtension(normalizedSceneName);
        if (string.IsNullOrWhiteSpace(sceneNameWithoutExtension))
        {
            return string.Empty;
        }

        return AssetDatabase
            .FindAssets("t:Scene")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(SceneQuickOpenUtility.IsValidScenePath)
            .Where(path => string.Equals(
                Path.GetFileNameWithoutExtension(path),
                sceneNameWithoutExtension,
                StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault() ?? string.Empty;
    }

    private static GamePhaseSceneEntry[] FilterScenes(GamePhaseSceneEntry[] scenes, string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return scenes;
        }

        return scenes
            .Where(scene =>
                scene.Label.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                scene.SceneName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                scene.Path.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                scene.Phase.ToString().IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
    }
}

internal static class SceneQuickOpenUtility
{
    public static SceneEntry[] FilterScenes(SceneEntry[] scenes, string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return scenes;
        }

        return scenes
            .Where(scene =>
                scene.Label.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                scene.Path.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
    }

    public static bool IsValidScenePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase);
    }

    public static string BuildLabel(string scenePath)
    {
        string normalized = scenePath.Replace('\\', '/');
        string noExtension = Path.ChangeExtension(normalized, null) ?? normalized;

        const string gameRoot = "Assets/Game/Scenes/";
        if (noExtension.StartsWith(gameRoot, StringComparison.OrdinalIgnoreCase))
        {
            return "Game/" + noExtension.Substring(gameRoot.Length);
        }

        const string sandboxRoot = "Assets/_Sandbox/";
        if (noExtension.StartsWith(sandboxRoot, StringComparison.OrdinalIgnoreCase))
        {
            return "Sandbox/" + noExtension.Substring(sandboxRoot.Length);
        }

        return noExtension.Replace("Assets/", "");
    }

    public static void OpenScene(string scenePath)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
    }
}

internal readonly struct SceneEntry
{
    public readonly string Path;
    public readonly string Label;

    public SceneEntry(string path, string label)
    {
        Path = path;
        Label = label;
    }
}

internal readonly struct GamePhaseSceneEntry
{
    public readonly GamePhase Phase;
    public readonly string SceneName;
    public readonly string Path;
    public readonly string Label;
    public readonly bool IsValid;

    public GamePhaseSceneEntry(GamePhase phase, string sceneName, string path, string label, bool isValid)
    {
        Phase = phase;
        SceneName = sceneName;
        Path = path;
        Label = label;
        IsValid = isValid;
    }
}
