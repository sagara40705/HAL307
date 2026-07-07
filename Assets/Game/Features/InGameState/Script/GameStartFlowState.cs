using System.Collections;

public sealed class GameStartFlowState : WaitSecondsFlowState
{
    public GameStartFlowState() : base(1f)
    {
    }

    public override IEnumerator Enter(InGameFlowContext context)
    {
        // Add game start presentation here.
        yield break;
    }
}
