using System.IO;
using UnityEditor;
using UnityEngine;

public class Workflow_ShowInExploer
{
    private const string MenuPath = "Tools/Workflow/Show In Exploer";
    private const int Priority = 100;

    [MenuItem(MenuPath, false, Priority)]
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
}
