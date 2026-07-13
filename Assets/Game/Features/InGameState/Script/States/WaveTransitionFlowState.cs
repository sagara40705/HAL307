using System.Collections;

// Wave変更演出State
public sealed class WaveTransitionFlowState : InGameFlowStateBase
{
    private readonly WaveData _completedWave;
    private readonly WaveData _nextWave;

    public WaveTransitionFlowState(WaveData completedWave)
        : this(completedWave, null)
    {
    }

    public WaveTransitionFlowState(WaveData completedWave, WaveData nextWave)
    {
        _completedWave = completedWave;
        _nextWave = nextWave;
    }

    public override IEnumerator Execute(InGameFlowContext context)
    {
        InGamePresentationAction action = _completedWave?.WaveTransitionAction;
        InGameFlowLock.BeginPresentation();
        try
        {
            if (action != null)
            {
                yield return action.Play(context);
            }

            // 固定時間ではなくダンベルの落下完了を待って次Waveへ進む
            yield return TransitionManager.WaitForTransitionCompletion();
        }
        finally
        {
            InGameFlowLock.EndPresentation();
        }
    }
}
