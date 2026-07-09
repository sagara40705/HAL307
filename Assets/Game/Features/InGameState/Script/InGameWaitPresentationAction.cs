using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "InGameWaitPresentationAction", menuName = "Game/InGame/Presentation/Wait")]
public sealed class InGameWaitPresentationAction : InGamePresentationAction
{
    [SerializeField, Min(0f)] private float seconds = 1f;

    // Waits for the configured duration.
    public override IEnumerator Play(InGameFlowContext context)
    {
        if (seconds <= 0f)
        {
            yield break;
        }

        yield return new WaitForSeconds(seconds);
    }
}
