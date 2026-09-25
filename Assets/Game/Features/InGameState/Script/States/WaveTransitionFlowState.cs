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
        InGameFlowLock.BeginPresentation();
        try
        {
            IEnumerator presentation = PlayTransitionActions(context);
            yield return context != null ? context.PlayWavePresentation(presentation) : presentation;
        }
        finally
        {
            InGameFlowLock.EndPresentation();
        }
    }

    private IEnumerator PlayTransitionActions(InGameFlowContext context)
    {
        InGamePresentationAction transitionAction = _completedWave?.WaveTransitionAction;
        InGamePresentationAction nextWavePrevAction = _nextWave?.WavePrevAction;
        if (transitionAction != null)
        {
            yield return transitionAction.Play(context);
        }

        if (nextWavePrevAction != null)
        {
            yield return nextWavePrevAction.Play(context);
        }

        // 固定時間ではなくダンベルの落下完了を待って次Waveへ進む
        yield return TransitionManager.WaitForTransitionCompletion();
    }
}
