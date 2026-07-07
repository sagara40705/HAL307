using System.Collections;

public sealed class GameEndFlowState : WaitSecondsFlowState
{
    public GameEndFlowState() : base(1f)
    {
    }

    public override IEnumerator Enter(InGameFlowContext context)
    {
        // Add game end presentation or result transition here.
        yield break;
    }
}
