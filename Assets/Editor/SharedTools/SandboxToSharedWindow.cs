using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class SandboxToSharedWindow : EditorWindow
{
    private readonly List<string> _candidateFolders = new List<string>();

    private Vector2 _scroll;
    private string _filter = string.Empty;
    private string _selectedSourcePath = string.Empty;
    private string _sharedFolderName = string.Empty;
    private string _sharedRootNamespace = string.Empty;

    [MenuItem("Tools/Shared/Promote Sandbox To Shared...", false, 120)]
    public static void Open()
    {
        SandboxToSharedWindow window = GetWindow<SandboxToSharedWindow>("Sandbox → Shared");
        window.minSize = new Vector2(560f, 430f);
        window.RefreshCandidates();
        window.TrySelectFromProjectWindow();
    }

    private void OnEnable()
    {
        RefreshCandidates();
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

        using (EditorGUILayout.ScrollViewScope scrollView = new EditorGUILayout.ScrollViewScope(_scroll, GUILayout.Height(190f)))
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
        _sharedRootNamespace = EditorGUILayout.TextField("Shared Root Namespace", _sharedRootNamespace);

        EditorGUILayout.HelpBox(
            "Shared 用 asmdef には Game と Unity.InputSystem を自動追加します。",
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
                "通常コピーは不可です。上書きする場合は Backup 付き Overwrite を使ってください。",
                MessageType.Warning
            );
        }

        using (new EditorGUI.DisabledScope(!hasSource || !hasSharedName))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(destinationExists))
                {
                    if (GUILayout.Button("Copy To _Shared", GUILayout.Height(32f)))
                    {
                        Promote(false);
                    }
                }

                using (new EditorGUI.DisabledScope(!destinationExists))
                {
                    if (GUILayout.Button("Overwrite With Backup", GUILayout.Height(32f)))
                    {
                        Promote(true);
                    }
                }
            }
        }
    }

    private void RefreshCandidates()
    {
        _candidateFolders.Clear();

        if (!AssetDatabase.IsValidFolder(SharedPromotionUtility.SandboxRoot))
        {
            return;
        }

        string[] directSubFolders = AssetDatabase.GetSubFolders(SharedPromotionUtility.SandboxRoot);
        foreach (string path in directSubFolders)
        {
            _candidateFolders.Add(path);
        }

        _candidateFolders.Sort(StringComparer.OrdinalIgnoreCase);
    }

    private List<string> GetFilteredCandidates()
    {
        if (string.IsNullOrWhiteSpace(_filter))
        {
            return new List<string>(_candidateFolders);
        }

        return _candidateFolders
            .Where(x => x.IndexOf(_filter, StringComparison.OrdinalIgnoreCase) >= 0)
            .ToList();
    }

    private void TrySelectFromProjectWindow()
    {
        if (Selection.activeObject == null)
        {
            return;
        }

        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        string root = SharedPromotionUtility.SandboxRoot + "/";
        if (!path.StartsWith(root, StringComparison.Ordinal))
        {
            return;
        }

        string relative = path.Substring(root.Length);
        string[] parts = relative.Split('/');

        if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
        {
            return;
        }

        string topLevelFolder = $"{SharedPromotionUtility.SandboxRoot}/{parts[0]}";
        if (AssetDatabase.IsValidFolder(topLevelFolder))
        {
            SelectSource(topLevelFolder);
        }
    }

    private void SelectSource(string path)
    {
        _selectedSourcePath = path;

        string folderName = Path.GetFileName(path);
        _sharedFolderName = SharedPromotionUtility.SanitizeName(folderName);
        _sharedRootNamespace = $"Shared.{SharedPromotionUtility.SanitizeNamespacePart(folderName)}";
    }

    private void Promote(bool overwriteWithBackup)
    {
        string sourcePath = _selectedSourcePath;
        string sharedFolderName = SharedPromotionUtility.SanitizeName(_sharedFolderName);
        string sharedRootNamespace = SharedPromotionUtility.SanitizeNamespace(_sharedRootNamespace);

        if (string.IsNullOrWhiteSpace(sourcePath) ||
            string.IsNullOrWhiteSpace(sharedFolderName) ||
            string.IsNullOrWhiteSpace(sharedRootNamespace))
        {
            EditorUtility.DisplayDialog("Error", "入力値が不正です。", "OK");
            return;
        }

        SharedPromotionUtility.EnsureFolder("Assets", "_Shared");

        string destinationPath = $"{SharedPromotionUtility.SharedRoot}/{sharedFolderName}";
        bool destinationExists = AssetDatabase.IsValidFolder(destinationPath);

        if (destinationExists && !overwriteWithBackup)
        {
            EditorUtility.DisplayDialog(
                "Error",
                $"既に存在します:\n{destinationPath}\n\n" +
                "上書きしたい場合は Overwrite With Backup を使ってください。",
                "OK"
            );
            return;
        }

        string gameAsmdefGuid = SharedPromotionUtility.FindAsmdefGuidByName("Game");
        if (string.IsNullOrEmpty(gameAsmdefGuid))
        {
            EditorUtility.DisplayDialog(
                "Error",
                "Game.asmdef が見つかりませんでした。Assets/Game/Game.asmdef を確認してください。",
                "OK"
            );
            return;
        }

        string sourceAsmdefPath = SharedPromotionUtility.FindFirstAsmdefPath(sourcePath);
        SharedPromotionUtility.AsmdefInfo sourceAsmdef = SharedPromotionUtility.ReadAsmdef(sourceAsmdefPath);
        string sourceRootNamespace = sourceAsmdef != null ? sourceAsmdef.rootNamespace : string.Empty;

        string backupPath = null;
        bool backupCreated = false;

        try
        {
            if (destinationExists)
            {
                SharedPromotionUtility.EnsureFolder(SharedPromotionUtility.SharedRoot, "_Backup");

                backupPath = SharedPromotionUtility.GenerateBackupPath(sharedFolderName);

                bool confirmed = EditorUtility.DisplayDialog(
                    "Overwrite With Backup",
                    $"既存フォルダをバックアップしてから上書きします。\n\n" +
                    $"対象:\n{destinationPath}\n\n" +
                    $"バックアップ先:\n{backupPath}",
                    "Overwrite",
                    "Cancel"
                );

                if (!confirmed)
                {
                    return;
                }

                string moveError = AssetDatabase.MoveAsset(destinationPath, backupPath);
                if (!string.IsNullOrEmpty(moveError))
                {
                    throw new Exception($"バックアップ作成に失敗しました。\n{moveError}");
                }

                backupCreated = true;
            }

            bool copied = AssetDatabase.CopyAsset(sourcePath, destinationPath);
            if (!copied)
            {
                throw new Exception("フォルダコピーに失敗しました。");
            }

            SharedPromotionUtility.DeleteAsmdefsInFolder(destinationPath);

            string sharedAsmdefName = $"{sharedFolderName}.Shared";
            SharedPromotionUtility.CreateAsmdef(destinationPath, sharedAsmdefName, sharedRootNamespace, gameAsmdefGuid);

            if (!string.IsNullOrWhiteSpace(sourceRootNamespace))
            {
                SharedPromotionUtility.ReplaceNamespaceInCSharpFiles(destinationPath, sourceRootNamespace, sharedRootNamespace);
            }

            SharedPromotionUtility.CreateOrOverwriteReadme(destinationPath, sourcePath, sharedFolderName, sharedRootNamespace);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            UnityEngine.Object folderObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(destinationPath);
            Selection.activeObject = folderObj;
            EditorGUIUtility.PingObject(folderObj);

            string message =
                $"完了:\n{destinationPath}" +
                (backupCreated ? $"\n\nBackup:\n{backupPath}" : string.Empty);

            EditorUtility.DisplayDialog("Completed", message, "OK");
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);

            AssetDatabase.Refresh();

            if (AssetDatabase.IsValidFolder(destinationPath))
            {
                AssetDatabase.DeleteAsset(destinationPath);
                AssetDatabase.Refresh();
            }

            if (backupCreated && !string.IsNullOrEmpty(backupPath) && AssetDatabase.IsValidFolder(backupPath))
            {
                string restoreError = AssetDatabase.MoveAsset(backupPath, destinationPath);
                if (!string.IsNullOrEmpty(restoreError))
                {
                    Debug.LogError($"バックアップからの復元に失敗しました: {restoreError}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog(
                "Error",
                $"Shared へのコピーに失敗しました。\n{ex.Message}\n\n" +
                "可能な限り元の状態へ復元を試みました。",
                "OK"
            );
        }
    }
}
