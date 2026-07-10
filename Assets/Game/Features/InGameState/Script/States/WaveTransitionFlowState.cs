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
        if (action != null)
        {
            InGameFlowLock.BeginPresentation();
            try
            {
                yield return action.Play(context);
            }
            finally
            {
                InGameFlowLock.EndPresentation();
            }
        }
    }
}
