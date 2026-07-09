using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "InGameUnityEventPresentationAction", menuName = "Game/InGame/Presentation/Unity Event")]
public sealed class InGameUnityEventPresentationAction : InGamePresentationAction
{
    [SerializeField] private string registeredEventName;
    [SerializeField, Min(0f)] private float waitAfterSeconds;

    public override IEnumerator Play(InGameFlowContext context)
    {
        InvokeRegisteredEvent(context);

        if (waitAfterSeconds <= 0f)
        {
            yield break;
        }

        yield return new WaitForSeconds(waitAfterSeconds);
    }

    private void InvokeRegisteredEvent(InGameFlowContext context)
    {
        if (string.IsNullOrWhiteSpace(registeredEventName))
        {
            return;
        }

        InGamePresentationEventRegistry registry = context != null ? context.PresentationEventRegistry : Object.FindFirstObjectByType<InGamePresentationEventRegistry>();
        if (registry == null)
        {
            Debug.LogWarning($"[InGameUnityEventPresentationAction] Event registry was not found. EventName={registeredEventName}");
            return;
        }

        registry.Invoke(registeredEventName);
    }
}
