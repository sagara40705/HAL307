using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public static class SharedPromotionUtility
{
    public const string SandboxRoot = "Assets/_Sandbox";
    public const string SharedRoot = "Assets/_Shared";
    public const string SharedBackupRoot = "Assets/_Shared/_Backup";

    public static void EnsureFolder(string parent, string folderName)
    {
        string target = $"{parent}/{folderName}";
        if (!AssetDatabase.IsValidFolder(target))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    public static string SanitizeName(string value)
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

    public static string SanitizeNamespace(string value)
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

    public static string SanitizeNamespacePart(string value)
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

    public static string GetAbsolutePath(string assetPath)
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        string normalized = assetPath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(projectRoot, normalized);
    }

    public static string FindAsmdefGuidByName(string asmdefName)
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

    public static string FindFirstAsmdefPath(string rootFolder)
    {
        string[] guids = AssetDatabase.FindAssets("t:asmdef", new[] { rootFolder });
        if (guids == null || guids.Length == 0)
        {
            return null;
        }

        return AssetDatabase.GUIDToAssetPath(guids[0]);
    }

    public static void DeleteAsmdefsInFolder(string rootFolder)
    {
        string[] guids = AssetDatabase.FindAssets("t:asmdef", new[] { rootFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AssetDatabase.DeleteAsset(path);
        }
    }

    public static string GenerateBackupPath(string sharedFolderName)
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

    public static void CreateAsmdef(string folderPath, string asmdefName, string rootNamespace, string gameAsmdefGuid)
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

    public static void ReplaceNamespaceInCSharpFiles(string rootFolder, string oldNamespace, string newNamespace)
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

    public static void CreateOrOverwriteReadme(
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
- Unity.InputSystem reference is included by default.
- Review scripts / scenes / prefabs before using in production.
";

        File.WriteAllText(GetAbsolutePath(readmePath), content);
    }

    [Serializable]
    public class AsmdefInfo
    {
        public string name;
        public string rootNamespace;
    }

    public static AsmdefInfo ReadAsmdef(string asmdefPath)
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

    public sealed class BackupEntry
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

    public static BackupEntry CreateBackupEntry(string assetPath)
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
        return match.Success ? match.Groups["base"].Value : folderName;
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
}
