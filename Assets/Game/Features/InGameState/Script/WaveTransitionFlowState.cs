using System.Collections;

// Wave transition presentation state.
public sealed class WaveTransitionFlowState : InGameFlowStateBase
{
    private readonly WaveData _completedWave;

    public WaveTransitionFlowState(WaveData completedWave)
    {
        _completedWave = completedWave;
    }

    // Plays the completed wave transition action.
    public override IEnumerator Execute(InGameFlowContext context)
    {
        InGamePresentationAction action = _completedWave?.WaveTransitionAction;
        if (action == null)
        {
            yield break;
        }

        yield return action.Play(context);
    }
}
