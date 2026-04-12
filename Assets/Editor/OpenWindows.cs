using System.IO;
using UnityEditor;
using UnityEngine;
using System.Diagnostics;

public class OpenWindows
{
    private const string MenuPath = "Tools/ OpenWindows";
    private const int Priority = 1;

    [MenuItem(MenuPath + "/Show In Exploer", false, Priority)]
    private static void ShowInExploer()
    {
        var projectRoot = Directory.GetParent(Application.dataPath)?.FullName;

        if (string.IsNullOrEmpty(projectRoot) || !Directory.Exists(projectRoot))
        {
            EditorUtility.DisplayDialog("Show In Exploer", "Project root folder not found.", "OK");
            return;
        }

        EditorUtility.RevealInFinder(projectRoot);
    }

    [MenuItem(MenuPath + "/GitHub Desktop", false, Priority + 1)]
    private static void OpenGitHubDesktop()
    {
        string exePath = @"C:\Users\%USERNAME%\AppData\Local\GitHubDesktop\GitHubDesktop.exe";
        try
        {
            exePath = System.Environment.ExpandEnvironmentVariables(exePath);

            if (!System.IO.File.Exists(exePath))
            {
                UnityEngine.Debug.LogError($"GitHub Desktop が見つかりません: {exePath}");
                return;
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                UseShellExecute = true
            });
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"GitHub Desktop の起動に失敗: {e}");
        }
    }
}
