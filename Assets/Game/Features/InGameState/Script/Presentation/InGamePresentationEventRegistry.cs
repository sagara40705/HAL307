using System;
using UnityEngine;
using UnityEngine.Events;

public sealed class InGamePresentationEventRegistry : MonoBehaviour
{
    [SerializeField] private EventEntry[] events;

    public static bool InvokeFromLoadedScenes(string eventName)
    {
        InGamePresentationEventRegistry[] registries = FindObjectsByType<InGamePresentationEventRegistry>(FindObjectsSortMode.None);
        if (registries == null || registries.Length <= 0)
        {
            Debug.LogWarning($"[InGamePresentationEventRegistry] Event registry was not found. EventName={eventName}");
            return false;
        }

        bool invoked = false;
        foreach (InGamePresentationEventRegistry registry in registries)
        {
            if (registry == null)
            {
                continue;
            }

            invoked |= registry.TryInvoke(eventName);
        }

        if (!invoked)
        {
            Debug.LogWarning($"[InGamePresentationEventRegistry] Event was not found. EventName={eventName}");
        }

        return invoked;
    }

    public bool Invoke(string eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName))
        {
            Debug.LogWarning("[InGamePresentationEventRegistry] Event name is empty.");
            return false;
        }

        if (events == null || events.Length <= 0)
        {
            Debug.LogWarning($"[InGamePresentationEventRegistry] Event list is empty. EventName={eventName}");
            return false;
        }

        foreach (EventEntry entry in events)
        {
            if (entry == null || !entry.IsMatch(eventName))
            {
                continue;
            }

            entry.Invoke();
            return true;
        }

        Debug.LogWarning($"[InGamePresentationEventRegistry] Event was not found. EventName={eventName}");
        return false;
    }

    public bool TryInvoke(string eventName)
    {
        if (string.IsNullOrWhiteSpace(eventName) || events == null || events.Length <= 0)
        {
            return false;
        }

        foreach (EventEntry entry in events)
        {
            if (entry == null || !entry.IsMatch(eventName))
            {
                continue;
            }

            entry.Invoke();
            return true;
        }

        return false;
    }

    [Serializable]
    private sealed class EventEntry
    {
        [SerializeField] private string eventName;
        [SerializeField] private UnityEvent onInvoke;

        public bool IsMatch(string targetName)
        {
            return string.Equals(eventName, targetName, StringComparison.Ordinal);
        }

        public void Invoke()
        {
            onInvoke?.Invoke();
        }
    }
}
