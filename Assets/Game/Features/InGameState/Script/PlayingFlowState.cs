using System.Collections;
using UnityEngine;

public sealed class PlayingFlowState : InGameFlowStateBase
{
    public override IEnumerator Execute(InGameFlowContext context)
    {
        StageEndJudge stageEndJudge = context != null ? context.StageEndJudge : null;
        if (stageEndJudge == null)
        {
            Debug.LogWarning("[PlayingFlowState] StageEndJudge is not assigned.");
            yield break;
        }

        bool isStageEnd = false;
        void OnStageEnd()
        {
            isStageEnd = true;
        }

        stageEndJudge.OnStageEnd += OnStageEnd;
        while (!isStageEnd)
        {
            yield return null;
        }

        stageEndJudge.OnStageEnd -= OnStageEnd;
    }
}
