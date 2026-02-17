using UnityEditor;

public static class Preflight_Run
{
    private const string Root = ToolsMenuConstants.Root + "Preflight/Run/";
    private const int PriorityBase = 200;

    [MenuItem(Root + "Fast", false,PriorityBase + 0)]
    private static void RunFast()
    {
        
    }

     [MenuItem(Root + "Full", false, PriorityBase + 1)]
         private static void RunFull()
    {
        
    }
}
