using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class SharedPromotionUtility
{
    public const string SandboxRoot = "Assets/_Sandbox";
    public const string SharedRoot = "Assets/_Shared";

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

        value = value.Replace("/", string.Empty);
        value = value.Replace("\\", string.Empty);
        value = value.Trim();

        return value;
    }

    public static List<string> GetDirectSubFolders(string rootFolder)
    {
        List<string> result = new List<string>();

        if (!AssetDatabase.IsValidFolder(rootFolder))
        {
            return result;
        }

        string[] subFolders = AssetDatabase.GetSubFolders(rootFolder);
        foreach (string subFolder in subFolders)
        {
            result.Add(subFolder);
        }

        result.Sort(System.StringComparer.OrdinalIgnoreCase);
        return result;
    }

    public static string GetTopLevelSandboxFolderFromPath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return null;
        }

        string prefix = SandboxRoot + "/";
        if (!path.StartsWith(prefix, System.StringComparison.Ordinal))
        {
            return null;
        }

        string relative = path.Substring(prefix.Length);
        if (string.IsNullOrEmpty(relative))
        {
            return null;
        }

        string[] parts = relative.Split('/');
        if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
        {
            return null;
        }

        string topLevelFolder = $"{SandboxRoot}/{parts[0]}";
        return AssetDatabase.IsValidFolder(topLevelFolder) ? topLevelFolder : null;
    }

    public static Object LoadFolderObject(string assetPath)
    {
        return AssetDatabase.LoadAssetAtPath<Object>(assetPath);
    }
}
