using UnityEngine.InputSystem;

public static class DebugShortcuts
{
    public static bool Enabled
    {
        get
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            return true;
#else
            return false;
#endif
        }
    }

    public static bool Pressed(Key key)
    {
        if (!Enabled)
        {
            return false;
        }

        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        return keyboard[key].wasPressedThisFrame;
    }

    public static bool Held(Key key)
    {
        if (!Enabled)
        {
            return false;
        }

        var keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return false;
        }

        return keyboard[key].isPressed;
    }

    public static bool Combo(Key holdKey, Key triggerKey)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        return Held(holdKey) && Pressed(triggerKey);
#else
        return false;
#endif
    }

    public static bool Combo(Key holdKey1, Key holdKey2, Key triggerKey)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        return Held(holdKey1) && Held(holdKey2) && Pressed(triggerKey);
#else
        return false;
#endif  
    }
}
