using System.Collections;
using UnityEngine;

// StageEndJudgeを待つ通常プレイState
public sealed class PlayingFlowState : InGameFlowStateBase
{
    private readonly WaveData _waveData;

    public PlayingFlowState(WaveData waveData)
    {
        _waveData = waveData;
    }

    // Wave開始演出の実行
    public override IEnumerator Enter(InGameFlowContext context)
    {
        context?.StageEndJudge?.ResetJudge();

        InGamePresentationAction action = _waveData?.WaveStartAction;
        if (action == null)
        {
            context?.ForwardProgressMover?.ResumeMove();
            yield break;
        }

        InGameFlowLock.BeginPresentation();
        try
        {
            yield return action.Play(context);
        }
        finally
        {
            InGameFlowLock.EndPresentation();
        }

        context?.ForwardProgressMover?.ResumeMove();
    }

    // StageEndJudgeの終了通知まで待機
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
