using System.Collections;

public sealed class WaveIntroFlowState : WaitSecondsFlowState
{
    public WaveIntroFlowState() : base(1f)
    {
    }

    public override IEnumerator Enter(InGameFlowContext context)
    {
        // Add wave intro presentation here.
        yield break;
    }
}
