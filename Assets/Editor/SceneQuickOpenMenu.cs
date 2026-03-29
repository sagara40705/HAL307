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
        "Assets/_Sandbox"
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
        SceneEntry[] filteredScenes = FilterScenes(_scenes, _searchText);

        if (filteredScenes.Length == 0)
        {
            EditorGUILayout.HelpBox("対象Sceneが見つかりません。", MessageType.Info);
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
                        OpenScene(scene.Path);
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
            .Where(IsValidScenePath)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .Select(path => new SceneEntry(path, BuildLabel(path)))
            .ToArray();

        Repaint();
    }

    private static SceneEntry[] FilterScenes(SceneEntry[] scenes, string searchText)
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

    private static bool IsValidScenePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        return path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildLabel(string scenePath)
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

    private void OpenScene(string scenePath)
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        Close();
    }

    private readonly struct SceneEntry
    {
        public readonly string Path;
        public readonly string Label;

        public SceneEntry(string path, string label)
        {
            Path = path;
            Label = label;
        }
    }
}
