using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class SandboxToSharedPromoter : EditorWindow
{
    private const string SandboxRoot = "Assets/_Sandbox";
    private const string SharedRoot = "Assets/_Shared";
    private const string SharedBackupRoot = "Assets/_Shared/_Backup";
    private const int DefaultKeepLatestBackupCount = 3;

    private readonly List<string> _candidateFolders = new List<string>();

    private Vector2 _scroll;
    private string _filter = string.Empty;
    private string _selectedSourcePath = string.Empty;
    private string _sharedFolderName = string.Empty;
    private string _sharedRootNamespace = string.Empty;

    [MenuItem("Tools/Shared/Promote Sandbox To Shared...", false, 120)]
    public static void Open()
    {
        SandboxToSharedPromoter window = GetWindow<SandboxToSharedPromoter>("Sandbox → Shared");
        window.minSize = new Vector2(560f, 430f);
        window.RefreshCandidates();
        window.TrySelectFromProjectWindow();
    }

    [MenuItem("Tools/Shared/Backups/Select Backup Folder", false, 121)]
    public static void SelectBackupFolder()
    {
        if (!AssetDatabase.IsValidFolder(SharedBackupRoot))
        {
            EditorUtility.DisplayDialog(
                "Info",
                $"{SharedBackupRoot} はまだ存在しません。",
                "OK"
            );
            return;
        }

        UnityEngine.Object folder = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(SharedBackupRoot);
        if (folder != null)
        {
            Selection.activeObject = folder;
            EditorGUIUtility.PingObject(folder);
        }
    }

    [MenuItem("Tools/Shared/Backups/Delete Old Backups (Keep Latest 3)...", false, 122)]
    public static void DeleteOldBackupsMenu()
    {
        DeleteOldBackupsInternal(DefaultKeepLatestBackupCount);
    }

    [MenuItem("Tools/Shared/Backups/Delete All Backups...", false, 123)]
    public static void DeleteAllBackupsMenu()
    {
        if (!AssetDatabase.IsValidFolder(SharedBackupRoot))
        {
            EditorUtility.DisplayDialog(
                "Info",
                $"{SharedBackupRoot} はまだ存在しません。",
                "OK"
            );
            return;
        }

        string[] backupFolders = AssetDatabase.GetSubFolders(SharedBackupRoot);
        if (backupFolders == null || backupFolders.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Info",
                "削除対象のバックアップはありません。",
                "OK"
            );
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "Delete All Backups",
            $"バックアップをすべて削除します。\n\n対象件数: {backupFolders.Length}",
            "Delete All",
            "Cancel"
        );

        if (!confirmed)
        {
            return;
        }

        int deletedCount = 0;
        foreach (string path in backupFolders)
        {
            if (AssetDatabase.DeleteAsset(path))
            {
                deletedCount++;
            }
            else
            {
                Debug.LogError($"バックアップ削除に失敗しました: {path}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Completed",
            $"削除完了: {deletedCount} 件",
            "OK"
        );
    }

    private void OnEnable()
    {
        RefreshCandidates();
    }

    private void OnGUI()
    {
        EditorGUILayout.Space();

        if (!AssetDatabase.IsValidFolder(SandboxRoot))
        {
            EditorGUILayout.HelpBox($"{SandboxRoot} が存在しません。", MessageType.Error);
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
            "推奨:\n" +
            "- 共有したい単位を1フォルダにまとめてから実行\n" +
            "- Script は namespace 付き推奨\n" +
            "- コピー先では既存 asmdef を削除し、新しい Shared 用 asmdef を作成\n" +
            "- rootNamespace が取得できた場合は .cs 内の namespace / using を Shared 側に置換",
            MessageType.None
        );
    }

    private void DrawExecuteArea()
    {
        bool hasSource = !string.IsNullOrWhiteSpace(_selectedSourcePath);
        bool hasSharedName = !string.IsNullOrWhiteSpace(_sharedFolderName);

        string destinationPath = $"{SharedRoot}/{SanitizeName(_sharedFolderName)}";
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
                        Promote(overwriteWithBackup: false);
                    }
                }

                using (new EditorGUI.DisabledScope(!destinationExists))
                {
                    if (GUILayout.Button("Overwrite With Backup", GUILayout.Height(32f)))
                    {
                        Promote(overwriteWithBackup: true);
                    }
                }
            }
        }
    }

    private void RefreshCandidates()
    {
        _candidateFolders.Clear();

        if (!AssetDatabase.IsValidFolder(SandboxRoot))
        {
            return;
        }

        string[] guids = AssetDatabase.FindAssets("t:folder", new[] { SandboxRoot });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path == SandboxRoot)
            {
                continue;
            }

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

        if (AssetDatabase.IsValidFolder(path) && path.StartsWith(SandboxRoot, StringComparison.Ordinal))
        {
            SelectSource(path);
        }
    }

    private void SelectSource(string path)
    {
        _selectedSourcePath = path;

        string folderName = Path.GetFileName(path);
        _sharedFolderName = SanitizeName(folderName);
        _sharedRootNamespace = $"Shared.{SanitizeNamespacePart(folderName)}";
    }

    private void Promote(bool overwriteWithBackup)
    {
        string sourcePath = _selectedSourcePath;
        string sharedFolderName = SanitizeName(_sharedFolderName);
        string sharedRootNamespace = SanitizeNamespace(_sharedRootNamespace);

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            EditorUtility.DisplayDialog("Error", "コピー元フォルダを選択してください。", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(sharedFolderName))
        {
            EditorUtility.DisplayDialog("Error", "Shared Folder Name が不正です。", "OK");
            return;
        }

        if (string.IsNullOrWhiteSpace(sharedRootNamespace))
        {
            EditorUtility.DisplayDialog("Error", "Shared Root Namespace が不正です。", "OK");
            return;
        }

        EnsureFolder("Assets", "_Shared");

        string destinationPath = $"{SharedRoot}/{sharedFolderName}";
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

        string gameAsmdefGuid = FindAsmdefGuidByName("Game");
        if (string.IsNullOrEmpty(gameAsmdefGuid))
        {
            EditorUtility.DisplayDialog(
                "Error",
                "Game.asmdef が見つかりませんでした。Assets/Game/Game.asmdef を確認してください。",
                "OK"
            );
            return;
        }

        string sourceAsmdefPath = FindFirstAsmdefPath(sourcePath);
        AsmdefInfo sourceAsmdef = ReadAsmdef(sourceAsmdefPath);
        string sourceRootNamespace = sourceAsmdef != null ? sourceAsmdef.rootNamespace : string.Empty;

        string backupPath = null;
        bool backupCreated = false;

        try
        {
            if (destinationExists)
            {
                EnsureFolder(SharedRoot, "_Backup");

                backupPath = GenerateBackupPath(sharedFolderName);

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

            DeleteAsmdefsInFolder(destinationPath);

            string sharedAsmdefName = $"{sharedFolderName}.Shared";
            CreateAsmdef(destinationPath, sharedAsmdefName, sharedRootNamespace, gameAsmdefGuid);

            if (!string.IsNullOrWhiteSpace(sourceRootNamespace))
            {
                ReplaceNamespaceInCSharpFiles(destinationPath, sourceRootNamespace, sharedRootNamespace);
            }

            CreateOrOverwriteReadme(destinationPath, sourcePath, sharedFolderName, sharedRootNamespace);

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

    private static void DeleteOldBackupsInternal(int keepLatestCount)
    {
        if (keepLatestCount < 0)
        {
            keepLatestCount = 0;
        }

        if (!AssetDatabase.IsValidFolder(SharedBackupRoot))
        {
            EditorUtility.DisplayDialog(
                "Info",
                $"{SharedBackupRoot} はまだ存在しません。",
                "OK"
            );
            return;
        }

        string[] subFolders = AssetDatabase.GetSubFolders(SharedBackupRoot);
        if (subFolders == null || subFolders.Length == 0)
        {
            EditorUtility.DisplayDialog(
                "Info",
                "削除対象のバックアップはありません。",
                "OK"
            );
            return;
        }

        List<BackupEntry> entries = subFolders
            .Select(CreateBackupEntry)
            .Where(x => x != null)
            .ToList();

        List<string> deleteTargets = new List<string>();

        foreach (IGrouping<string, BackupEntry> group in entries.GroupBy(x => x.GroupName))
        {
            List<BackupEntry> ordered = group
                .OrderByDescending(x => x.Timestamp)
                .ThenByDescending(x => x.AssetPath, StringComparer.OrdinalIgnoreCase)
                .ToList();

            deleteTargets.AddRange(ordered.Skip(keepLatestCount).Select(x => x.AssetPath));
        }

        if (deleteTargets.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Info",
                $"各グループの最新 {keepLatestCount} 件を残した結果、削除対象はありません。",
                "OK"
            );
            return;
        }

        bool confirmed = EditorUtility.DisplayDialog(
            "Delete Old Backups",
            $"各グループの最新 {keepLatestCount} 件を残して古いバックアップを削除します。\n\n" +
            $"削除対象: {deleteTargets.Count} 件",
            "Delete",
            "Cancel"
        );

        if (!confirmed)
        {
            return;
        }

        int deletedCount = 0;
        foreach (string path in deleteTargets)
        {
            if (AssetDatabase.DeleteAsset(path))
            {
                deletedCount++;
            }
            else
            {
                Debug.LogError($"バックアップ削除に失敗しました: {path}");
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "Completed",
            $"削除完了: {deletedCount} 件",
            "OK"
        );
    }

    private static BackupEntry CreateBackupEntry(string assetPath)
    {
        string folderName = Path.GetFileName(assetPath);
        string groupName = ExtractBackupGroupName(folderName);
        DateTime timestamp = ExtractBackupTimestamp(folderName);

        if (timestamp == DateTime.MinValue)
        {
            string absolutePath = GetAbsolutePath(assetPath);
            if (Directory.Exists(absolutePath))
            {
                timestamp = Directory.GetLastWriteTime(absolutePath);
            }
        }

        return new BackupEntry(assetPath, groupName, timestamp);
    }

    private static string ExtractBackupGroupName(string folderName)
    {
        Match match = Regex.Match(folderName, @"^(?<base>.+)_\d{8}_\d{6}(?:_\d+)?$");
        if (match.Success)
        {
            return match.Groups["base"].Value;
        }

        return folderName;
    }

    private static DateTime ExtractBackupTimestamp(string folderName)
    {
        Match match = Regex.Match(folderName, @"_(?<date>\d{8})_(?<time>\d{6})(?:_\d+)?$");
        if (!match.Success)
        {
            return DateTime.MinValue;
        }

        string raw = match.Groups["date"].Value + match.Groups["time"].Value;
        if (DateTime.TryParseExact(
                raw,
                "yyyyMMddHHmmss",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out DateTime parsed))
        {
            return parsed;
        }

        return DateTime.MinValue;
    }

    private static void EnsureFolder(string parent, string folderName)
    {
        string target = $"{parent}/{folderName}";
        if (!AssetDatabase.IsValidFolder(target))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    private static string GenerateBackupPath(string sharedFolderName)
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string baseName = $"{sharedFolderName}_{timestamp}";
        string path = $"{SharedBackupRoot}/{baseName}";

        int index = 1;
        while (AssetDatabase.IsValidFolder(path))
        {
            path = $"{SharedBackupRoot}/{baseName}_{index}";
            index++;
        }

        return path;
    }

    private static string FindAsmdefGuidByName(string asmdefName)
    {
        string[] guids = AssetDatabase.FindAssets($"{asmdefName} t:asmdef");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);

            if (fileName == asmdefName)
            {
                return guid;
            }
        }

        return null;
    }

    private static string FindFirstAsmdefPath(string rootFolder)
    {
        string[] guids = AssetDatabase.FindAssets("t:asmdef", new[] { rootFolder });
        if (guids == null || guids.Length == 0)
        {
            return null;
        }

        return AssetDatabase.GUIDToAssetPath(guids[0]);
    }

    private static void DeleteAsmdefsInFolder(string rootFolder)
    {
        string[] guids = AssetDatabase.FindAssets("t:asmdef", new[] { rootFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AssetDatabase.DeleteAsset(path);
        }
    }

    private static void CreateAsmdef(string folderPath, string asmdefName, string rootNamespace, string gameAsmdefGuid)
    {
        string asmdefPath = $"{folderPath}/{asmdefName}.asmdef";

        string json =
$@"{{
    ""name"": ""{asmdefName}"",
    ""rootNamespace"": ""{rootNamespace}"",
    ""references"": [
        ""GUID:{gameAsmdefGuid}"",
        ""Unity.InputSystem""
    ],
    ""includePlatforms"": [],
    ""excludePlatforms"": [],
    ""allowUnsafeCode"": false,
    ""overrideReferences"": false,
    ""precompiledReferences"": [],
    ""autoReferenced"": false,
    ""defineConstraints"": [],
    ""versionDefines"": [],
    ""noEngineReferences"": false
}}";

        File.WriteAllText(GetAbsolutePath(asmdefPath), json);
    }

    private static void ReplaceNamespaceInCSharpFiles(string rootFolder, string oldNamespace, string newNamespace)
    {
        string absoluteRoot = GetAbsolutePath(rootFolder);
        if (!Directory.Exists(absoluteRoot))
        {
            return;
        }

        string[] files = Directory.GetFiles(absoluteRoot, "*.cs", SearchOption.AllDirectories);
        foreach (string filePath in files)
        {
            string original = File.ReadAllText(filePath);
            string replaced = original;

            replaced = ReplaceNamespaceDeclaration(replaced, oldNamespace, newNamespace);
            replaced = ReplaceUsingDirective(replaced, oldNamespace, newNamespace);

            if (replaced != original)
            {
                File.WriteAllText(filePath, replaced);
            }
        }
    }

    private static string ReplaceNamespaceDeclaration(string source, string oldNamespace, string newNamespace)
    {
        string pattern =
            $@"\bnamespace\s+{Regex.Escape(oldNamespace)}(?<suffix>(\.[A-Za-z_][A-Za-z0-9_]*)*)\b";

        return Regex.Replace(
            source,
            pattern,
            match => $"namespace {newNamespace}{match.Groups["suffix"].Value}"
        );
    }

    private static string ReplaceUsingDirective(string source, string oldNamespace, string newNamespace)
    {
        string pattern =
            $@"\busing\s+{Regex.Escape(oldNamespace)}(?<suffix>(\.[A-Za-z_][A-Za-z0-9_]*)*)\s*;";

        return Regex.Replace(
            source,
            pattern,
            match => $"using {newNamespace}{match.Groups["suffix"].Value};"
        );
    }

    private static void CreateOrOverwriteReadme(
        string rootFolder,
        string sourcePath,
        string sharedFolderName,
        string sharedRootNamespace)
    {
        string readmePath = $"{rootFolder}/README.md";

        string content =
$@"# {sharedFolderName}

- Source: `{sourcePath}`
- Namespace: `{sharedRootNamespace}`

## Notes
- This folder was promoted from `_Sandbox`.
- Existing asmdef files from source were removed.
- A new Shared asmdef was created automatically.
- Review scripts / scenes / prefabs before using in production.
";

        File.WriteAllText(GetAbsolutePath(readmePath), content);
    }

    private static string GetAbsolutePath(string assetPath)
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string normalized = assetPath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(projectRoot, normalized);
    }

    private static string SanitizeName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        foreach (char c in Path.GetInvalidFileNameChars())
        {
            value = value.Replace(c.ToString(), string.Empty);
        }

        value = value.Replace(" ", string.Empty);
        value = value.Replace(".", string.Empty);
        value = value.Replace("/", string.Empty);
        value = value.Replace("\\", string.Empty);

        return value.Trim();
    }

    private static string SanitizeNamespace(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string[] parts = value.Split('.');
        List<string> sanitizedParts = new List<string>();

        foreach (string part in parts)
        {
            string sanitized = SanitizeNamespacePart(part);
            if (!string.IsNullOrWhiteSpace(sanitized))
            {
                sanitizedParts.Add(sanitized);
            }
        }

        return string.Join(".", sanitizedParts);
    }

    private static string SanitizeNamespacePart(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string sanitized = Regex.Replace(value, @"[^A-Za-z0-9_]", string.Empty);
        if (string.IsNullOrEmpty(sanitized))
        {
            return string.Empty;
        }

        if (char.IsDigit(sanitized[0]))
        {
            sanitized = "_" + sanitized;
        }

        return sanitized;
    }

    [Serializable]
    private class AsmdefInfo
    {
        public string name;
        public string rootNamespace;
    }

    private static AsmdefInfo ReadAsmdef(string asmdefPath)
    {
        if (string.IsNullOrWhiteSpace(asmdefPath))
        {
            return null;
        }

        string absolutePath = GetAbsolutePath(asmdefPath);
        if (!File.Exists(absolutePath))
        {
            return null;
        }

        string json = File.ReadAllText(absolutePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonUtility.FromJson<AsmdefInfo>(json);
        }
        catch
        {
            return null;
        }
    }

    private sealed class BackupEntry
    {
        public string AssetPath { get; }
        public string GroupName { get; }
        public DateTime Timestamp { get; }

        public BackupEntry(string assetPath, string groupName, DateTime timestamp)
        {
            AssetPath = assetPath;
            GroupName = groupName;
            Timestamp = timestamp;
        }
    }
}
