using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InGameSetActorsIdlePresentationAction", menuName = "Game/InGame/Presentation/Set Actors Idle")]
public sealed class InGameSetActorsIdlePresentationAction : InGamePresentationAction
{
    public override IEnumerator Play(InGameFlowContext context)
    {
        HashSet<OnStartPose> targets = InGameActorAnimationTargetResolver.Collect(context);

        if (targets.Count <= 0)
        {
            Debug.LogWarning("[InGameSetActorsIdlePresentationAction] OnStartPose was not found under Player or Enemy roots.");
            yield break;
        }

        foreach (OnStartPose target in targets)
        {
            if (target != null)
            {
                target.SetTriggerIdle();
            }
        }
    }
}
