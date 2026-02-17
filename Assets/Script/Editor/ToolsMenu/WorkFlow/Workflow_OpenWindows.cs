using UnityEditor;

public class Workflow_OpenWindows
{
    private const string Root = ToolsMenuConstants.Root + "Workflow/Open Windows/";
    private const int PriorityBase = 115;


    [MenuItem(Root + "Project Settings", false, PriorityBase + 0)]
 private static void OpenProjectSettings()
    {
#if UNITY_2018_3_OR_NEWER
        SettingsService.OpenProjectSettings();
#else
        EditorApplication.ExecuteMenuItem("Edit/Project Settings...");
#endif
    }


    [MenuItem(Root + "Preferences", false, PriorityBase + 1)]
    private static void OpenPreferences()
    {
#if UNITY_2018_3_OR_NEWER
        SettingsService.OpenUserPreferences();
#else
        if (!EditorApplication.ExecuteMenuItem("Edit/Preferences..."))
            EditorApplication.ExecuteMenuItem("Unity/Preferences...");
#endif
    }


    [MenuItem(Root + "Package Manager", false, PriorityBase + 2)]
private static void OpenPackageManager()
    {
        if (!EditorApplication.ExecuteMenuItem("Window/Package Management/Package Manager"))
            EditorApplication.ExecuteMenuItem("Window/Package Management/Package Manager...");
    }


    [MenuItem(Root + "Build Profiles", false, PriorityBase + 3)]
    private static void OpenBuildProfiles()
    {
        if (!EditorApplication.ExecuteMenuItem("File/Build Profiles"))
            EditorApplication.ExecuteMenuItem("File/Build Profiles...");
    }


    [MenuItem(Root + "Profiler", false, PriorityBase + 4)]
    private static void OpenProfiler()
    {
        if (!EditorApplication.ExecuteMenuItem("Window/Analysis/Profiler"))
            EditorApplication.ExecuteMenuItem("Window/Analysis/Profiler...");
    }


    [MenuItem(Root + "Console", false, PriorityBase + 5)]
    private static void OpenConsole()
    {
        if (!EditorApplication.ExecuteMenuItem("Window/General/Console"))
            EditorApplication.ExecuteMenuItem("Window/Console");
    }
}
