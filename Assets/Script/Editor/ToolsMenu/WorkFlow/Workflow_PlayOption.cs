using UnityEditor;

public class Workflow_PlayOption
{
    private const string Root = ToolsMenuConstants.Root + "Workflow/Play Option/";
    private const int PriorityBase = 145;
    private const string StateAssetFilter = "t:PlayOptionState";

    private static PlayOptionState _cachedState;
    private static string _cachedStatePath;

    [MenuItem(Root + "State Inspector", false, PriorityBase + 0)]
    private static void StateInspector()
    {
        ViewStateInspector();
    }

    [MenuItem(Root + "Enter Play Mode", false, PriorityBase + 11)]
    private static void EnterPlayMode()
    {
        ToggleFlag("Enter Play Mode");
    }

    [MenuItem(Root + "Enter Play Mode", true, PriorityBase + 11)]
    private static bool EnterPlayMode_Validate()
    {
        return SetMenuChecked("Enter Play Mode");
    }

    [MenuItem(Root + "Maximize Game View", false, PriorityBase + 12)]
    private static void MaximizeGameView()
    {
        ToggleFlag("Maximize Game View");
    }

    [MenuItem(Root + "Maximize Game View", true, PriorityBase + 12)]
    private static bool MaximizeGameView_Validate()
    {
        return SetMenuChecked("Maximize Game View");
    }

    [MenuItem(Root + "Muto Audio", false, PriorityBase + 13)]
    private static void MutoAudio()
    {
        ToggleFlag("Muto Audio");
    }

    [MenuItem(Root + "Muto Audio", true, PriorityBase + 13)]
    private static bool MutoAudio_Validate()
    {
        return SetMenuChecked("Muto Audio");
    }

    [MenuItem(Root + "Auto Connect Profiler", false, PriorityBase + 14)]
    private static void AutoConnectProfiler()
    {
        ToggleFlag("Auto Connect Profiler");
    }

    [MenuItem(Root + "Auto Connect Profiler", true, PriorityBase + 14)]
    private static bool AutoConnectProfiler_Validate()
    {
        return SetMenuChecked("Auto Connect Profiler");
    }

    [MenuItem(Root + "Deep Profiling", false, PriorityBase + 15)]
    private static void DeepProfiling()
    {
        ToggleFlag("Deep Profiling");
    }

    [MenuItem(Root + "Deep Profiling", true, PriorityBase + 15)]
    private static bool DeepProfiling_Validate()
    {
        return SetMenuChecked("Deep Profiling");
    }

    private static bool SetMenuChecked(string key)
    {
        var menuPath = Root + key;
        var state = GetState();
        var isOn = state != null && state.GetFlag(key);
        Menu.SetChecked(menuPath, isOn);
        return true;
    }

    private static void ToggleFlag(string key)
    {
        var state = GetState();
        if (state == null) return;

        if (state.TryToggleState(key, out var value))
        {
            Menu.SetChecked(Root + key, value);
            EditorUtility.SetDirty(state);
        }
    }

    private static void ViewStateInspector()
    {
        var state = GetState();
        if (state == null) return;

        Selection.activeObject = state;
        EditorGUIUtility.PingObject(state);
    }

    private static PlayOptionState GetState()
    {
        if (_cachedState != null) return _cachedState;

        if (!string.IsNullOrEmpty(_cachedStatePath))
        {
            _cachedState = AssetDatabase.LoadAssetAtPath<PlayOptionState>(_cachedStatePath);
            if (_cachedState != null) return _cachedState;
        }

        var guids = AssetDatabase.FindAssets(StateAssetFilter);
        if (guids == null || guids.Length == 0) return null;

        _cachedStatePath = AssetDatabase.GUIDToAssetPath(guids[0]);
        _cachedState = AssetDatabase.LoadAssetAtPath<PlayOptionState>(_cachedStatePath);
        return _cachedState;
    }
}
