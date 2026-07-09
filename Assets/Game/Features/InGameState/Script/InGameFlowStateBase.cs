using System.Collections;

// Base class for in-game flow states.
public abstract class InGameFlowStateBase : IInGameFlowState
{
    // Uses class name as default state name.
    public virtual string StateName => GetType().Name;

    // Default enter logic.
    public virtual IEnumerator Enter(InGameFlowContext context)
    {
        yield break;
    }

    // Main state body.
    public abstract IEnumerator Execute(InGameFlowContext context);

    // Default exit logic.
    public virtual IEnumerator Exit(InGameFlowContext context)
    {
        yield break;
    }
}
