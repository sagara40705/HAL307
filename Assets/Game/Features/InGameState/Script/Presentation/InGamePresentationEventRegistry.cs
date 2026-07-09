using System;
using UnityEngine;
using UnityEngine.Events;

public sealed class InGamePresentationEventRegistry : MonoBehaviour
{
    [SerializeField] private EventEntry[] events;

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
