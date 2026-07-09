using System.Collections;

// Wave変更演出State
public sealed class WaveTransitionFlowState : InGameFlowStateBase
{
    private readonly WaveData _completedWave;

    public WaveTransitionFlowState(WaveData completedWave)
    {
        _completedWave = completedWave;
    }

    // 完了したWaveの変更演出を実行
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
