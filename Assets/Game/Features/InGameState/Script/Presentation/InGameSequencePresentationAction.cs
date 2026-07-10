using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "InGameSequencePresentationAction", menuName = "Game/InGame/Presentation/Sequence")]
public sealed class InGameSequencePresentationAction : InGamePresentationAction
{
    [SerializeField] private InGamePresentationAction[] actions;

    // 登録順に演出を実行
    public override IEnumerator Play(InGameFlowContext context)
    {
        if (actions == null || actions.Length <= 0)
        {
            yield break;
        }

        foreach (InGamePresentationAction action in actions)
        {
            if (action == null)
            {
                continue;
            }

            if (ReferenceEquals(action, this))
            {
                Debug.LogWarning("[InGameSequencePresentationAction] Self reference was skipped.");
                continue;
            }

            yield return action.Play(context);
        }
    }
}
