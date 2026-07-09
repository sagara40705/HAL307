using System.Collections;
using UnityEngine;

// Gameplay state that waits for StageEndJudge.
public sealed class PlayingFlowState : InGameFlowStateBase
{
    private readonly WaveData _waveData;

    public PlayingFlowState(WaveData waveData)
    {
        _waveData = waveData;
    }

    // Plays optional wave start presentation.
    public override IEnumerator Enter(InGameFlowContext context)
    {
        context?.StageEndJudge?.ResetJudge();

        InGamePresentationAction action = _waveData?.WaveStartAction;
        if (action == null)
        {
            context?.ForwardProgressMover?.ResumeMove();
            yield break;
        }

        yield return action.Play(context);
        context?.ForwardProgressMover?.ResumeMove();
    }

    // Waits until StageEndJudge emits OnStageEnd.
    public override IEnumerator Execute(InGameFlowContext context)
    {
        StageEndJudge stageEndJudge = context != null ? context.StageEndJudge : null;
        if (stageEndJudge == null)
        {
            Debug.LogWarning("[PlayingFlowState] StageEndJudge is not assigned.");
            yield break;
        }

        bool isStageEnd = false;

        // Stage end callback.
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
