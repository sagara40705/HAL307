// Assets/Editor/ProjectToolsMenu.cs
using UnityEditor;

public static class ProjectToolsMenu
{
    private const string Root = ToolsMenuConstants.Root;

    // 000–099：Dashboard / Quick
    [MenuItem(Root + "Dashboard", priority = 0)]
    private static void Dashboard() { }

    [MenuItem(Root + "Quick/Placeholder", priority = 10)]
    private static void QuickPlaceholder() { }

    // 100–199：Setup
    [MenuItem(Root + "Setup/Placeholder", priority = 50)]
    private static void SetupPlaceholder() { }

    // 200–299：Preflight

    // 300–399：Tests
    [MenuItem(Root + "Tests/Placeholder", priority = 300)]
    private static void TestsPlaceholder() { }

    // 400–499：Build
    [MenuItem(Root + "Build/Placeholder", priority = 400)]
    private static void BuildPlaceholder() { }

    // 500–599：Content（Addressables等）
    [MenuItem(Root + "Content/Placeholder", priority = 500)]
    private static void ContentPlaceholder() { }

    // 600–699：Data/Cache
    [MenuItem(Root + "Data & Cache/Placeholder", priority = 600)]
    private static void DataCachePlaceholder() { }

    // 900–999：Help
    [MenuItem(Root + "Help/Placeholder", priority = 900)]
    private static void HelpPlaceholder() { }
}
