using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public sealed class RuntimeDebugManager : AppManagerBase<RuntimeDebugManager>
{
    [SerializeField] private bool enableToggleShortcut = true;

    protected override IEnumerator OnInitialize()
    {
        Debug.Log("[RuntimeDebugManager] Initialize completed.");
        yield break;
    }

    private void Update()
    {
        if (!IsInitialized)
        {
            return;
        }

        if (!enableToggleShortcut)
        {
            return;
        }

        if (DebugShortcuts.Combo(Key.D, Key.M))
        {
            RuntimeDebug.DebugMenu.Toggle();
        }
    }

    private void OnGUI()
    {
        if (!IsInitialized)
        {
            return;
        }

        RuntimeDebug.DebugMenu.Draw();
    }
}
