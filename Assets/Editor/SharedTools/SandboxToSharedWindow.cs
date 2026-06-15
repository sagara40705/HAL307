using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class SandboxToSharedWindow : EditorWindow
{
    private readonly List<string> _candidateFolders = new List<string>();

    private Vector2 _scroll;
    private string _filter = string.Empty;
    private string _selectedSourcePath = string.Empty;
    private string _sharedFolderName = string.Empty;

    [MenuItem("Tools/Shared/Promote Sandbox To Shared...", false, 120)]
    public static void Open()
    {
        SandboxToSharedWindow window = GetWindow<SandboxToSharedWindow>("Sandbox → Shared");
        window.minSize = new Vector2(560f, 360f);
        window.RefreshCandidates();
        window.TrySelectFromProjectWindow();
    }

    private void OnEnable()
    {
        RefreshCandidates();
        TrySelectFromProjectWindow();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();

        if (!AssetDatabase.IsValidFolder(SharedPromotionUtility.SandboxRoot))
        {
            EditorGUILayout.HelpBox($"{SharedPromotionUtility.SandboxRoot} が存在しません。", MessageType.Error);
            return;
        }

        DrawSearchArea();
        EditorGUILayout.Space(8f);
        DrawCandidateList();
        EditorGUILayout.Space(8f);
        DrawSettingsArea();
        EditorGUILayout.Space(12f);
        DrawExecuteArea();
    }

    private void DrawSearchArea()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Filter", GUILayout.Width(45f));

            string newFilter = EditorGUILayout.TextField(_filter);
            if (newFilter != _filter)
            {
                _filter = newFilter;
            }

            if (GUILayout.Button("Refresh", GUILayout.Width(90f)))
            {
                RefreshCandidates();
                TrySelectFromProjectWindow();
            }
        }
    }

    private void DrawCandidateList()
    {
        EditorGUILayout.LabelField("Sandbox Folders", EditorStyles.boldLabel);

        List<string> filtered = GetFilteredCandidates();

        using (EditorGUILayout.ScrollViewScope scrollView = new EditorGUILayout.ScrollViewScope(_scroll, GUILayout.Height(180f)))
        {
            _scroll = scrollView.scrollPosition;

            if (filtered.Count == 0)
            {
                EditorGUILayout.HelpBox("候補フォルダがありません。", MessageType.Info);
                return;
            }

            foreach (string path in filtered)
            {
                bool isSelected = path == _selectedSourcePath;
                bool pressed = GUILayout.Toggle(isSelected, path, "Button");

                if (pressed && !isSelected)
                {
                    SelectSource(path);
                }
            }
        }
    }

    private void DrawSettingsArea()
    {
        EditorGUILayout.LabelField("Promotion Settings", EditorStyles.boldLabel);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.TextField("Source", string.IsNullOrEmpty(_selectedSourcePath) ? "(none)" : _selectedSourcePath);
        }

        _sharedFolderName = EditorGUILayout.TextField("Shared Folder Name", _sharedFolderName);

        EditorGUILayout.HelpBox(
            "この処理は Copy ではなく Move です。\n" +
            "- _Sandbox から _Shared へ移動します\n" +
            "- 上書きはしません\n" +
            "- Script / namespace はそのまま保持します",
            MessageType.None
        );
    }

    private void DrawExecuteArea()
    {
        bool hasSource = !string.IsNullOrWhiteSpace(_selectedSourcePath);
        bool hasSharedName = !string.IsNullOrWhiteSpace(_sharedFolderName);

        string destinationPath = $"{SharedPromotionUtility.SharedRoot}/{SharedPromotionUtility.SanitizeName(_sharedFolderName)}";
        bool destinationExists = hasSharedName && AssetDatabase.IsValidFolder(destinationPath);

        if (destinationExists)
        {
            EditorGUILayout.HelpBox(
                $"同名フォルダが既に存在します:\n{destinationPath}\n" +
                "上書きせず停止します。別名に変更してください。",
                MessageType.Warning
            );
        }

        using (new EditorGUI.DisabledScope(!hasSource || !hasSharedName || destinationExists))
        {
            if (GUILayout.Button("Move To _Shared", GUILayout.Height(32f)))
            {
                Promote();
            }
        }
    }

    private void RefreshCandidates()
    {
        _candidateFolders.Clear();
        _candidateFolders.AddRange(SharedPromotionUtility.GetDirectSubFolders(SharedPromotionUtility.SandboxRoot));
    }

    private List<string> GetFilteredCandidates()
    {
        if (string.IsNullOrWhiteSpace(_filter))
        {
            return new List<string>(_candidateFolders);
        }

        List<string> filtered = new List<string>();

        foreach (string candidate in _candidateFolders)
        {
            if (candidate.IndexOf(_filter, System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                filtered.Add(candidate);
            }
        }

        return filtered;
    }

    private void TrySelectFromProjectWindow()
    {
        if (Selection.activeObject == null)
        {
            return;
        }

        string selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(selectedPath))
        {
            return;
        }

        string topLevelFolder = SharedPromotionUtility.GetTopLevelSandboxFolderFromPath(selectedPath);
        if (!string.IsNullOrEmpty(topLevelFolder))
        {
            SelectSource(topLevelFolder);
        }
    }

    private void SelectSource(string path)
    {
        _selectedSourcePath = path;
        _sharedFolderName = SharedPromotionUtility.SanitizeName(Path.GetFileName(path));
    }

    private void Promote()
    {
        string sourcePath = _selectedSourcePath;
        string sharedFolderName = SharedPromotionUtility.SanitizeName(_sharedFolderName);

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            EditorUtility.DisplayDialog("Error", "移動元フォルダを選択してください。", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(sharedFolderName))
        {
            EditorUtility.DisplayDialog("Error", "Shared Folder Name が不正です。", "OK");
            return;
        }

        SharedPromotionUtility.EnsureFolder("Assets", "_Shared");

        string destinationPath = $"{SharedPromotionUtility.SharedRoot}/{sharedFolderName}";
        if (AssetDatabase.IsValidFolder(destinationPath))
        {
            EditorUtility.DisplayDialog(
                "Error",
                $"同名フォルダが既に存在します:\n{destinationPath}\n\n別名を指定してください。",
                "OK"
            );
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "Move To _Shared",
            $"次のフォルダを _Shared へ移動します。\n\n" +
            $"From:\n{sourcePath}\n\n" +
            $"To:\n{destinationPath}\n\n" +
            "この処理は Move です。_Sandbox 側からは消えます。",
            "Move",
            "Cancel"
        );

        if (!confirmed)
        {
            return;
        }

        string moveError = AssetDatabase.MoveAsset(sourcePath, destinationPath);
        if (!string.IsNullOrEmpty(moveError))
        {
            EditorUtility.DisplayDialog(
                "Error",
                $"移動に失敗しました。\n{moveError}",
                "OK"
            );
            return;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Object folderObj = SharedPromotionUtility.LoadFolderObject(destinationPath);
        if (folderObj != null)
        {
            Selection.activeObject = folderObj;
            EditorGUIUtility.PingObject(folderObj);
        }

        RefreshCandidates();

        EditorUtility.DisplayDialog(
            "Completed",
            $"移動完了:\n{destinationPath}",
            "OK"
        );
    }
}
