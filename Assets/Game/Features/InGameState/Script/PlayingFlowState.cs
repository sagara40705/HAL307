using System.Collections;
using UnityEngine;

// 通常プレイ中State
public sealed class PlayingFlowState : InGameFlowStateBase
{
    // ステージ終了通知まで待機
    public override IEnumerator Execute(InGameFlowContext context)
    {
        StageEndJudge stageEndJudge = context != null ? context.StageEndJudge : null;
        if (stageEndJudge == null)
        {
            Debug.LogWarning("[PlayingFlowState] StageEndJudge is not assigned.");
            yield break;
        }

        bool isStageEnd = false;

        // ステージ終了通知の受信
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
