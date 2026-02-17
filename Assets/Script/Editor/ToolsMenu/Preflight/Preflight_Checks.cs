using UnityEditor;


public static class Preflight_Checks
{
    private const string Root = ToolsMenuConstants.Root + "Preflight/Checks/";
    private const int PriorityBase = 220;

    [MenuItem(Root + "Sample", false, PriorityBase + 0)]
    private static void CheckSample()
    {
        
    }
}
