using System.Collections;

public sealed class WaveEndFlowState : WaitSecondsFlowState
{
    public WaveEndFlowState() : base(1f)
    {
    }

    public override IEnumerator Enter(InGameFlowContext context)
    {
        // Add wave end presentation here.
        yield break;
    }
}
