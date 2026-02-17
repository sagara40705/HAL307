using UnityEditor;


public static class Preflight_Fix
{    
    private const string Root = ToolsMenuConstants.Root + "Preflight/Fix/";
    private const int PriorityBase = 260;

    [MenuItem(Root + "Sample", false, PriorityBase + 0)]
    private static void FixSample()
    {
        
    }
}
