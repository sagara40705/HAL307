using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "InGamePlayRandomActorPosePresentationAction", menuName = "Game/InGame/Presentation/Play Random Actor Pose")]
public sealed class InGamePlayRandomActorPosePresentationAction : InGamePresentationAction
{
    [SerializeField] private bool includeEnemies = true;
    [SerializeField] private bool includePlayer = true;

    public override IEnumerator Play(InGameFlowContext context)
    {
        HashSet<OnStartPose> targets = InGameActorAnimationTargetResolver.Collect(
            context,
            includeEnemies,
            includePlayer);
        if (targets.Count <= 0)
        {
            Debug.LogWarning("[InGamePlayRandomActorPosePresentationAction] OnStartPose was not found under Player or Enemy roots.");
            yield break;
        }

        foreach (OnStartPose target in targets)
        {
            if (target != null)
            {
                target.PlayRandomPoseAnimation();
            }
        }
    }
}
