using System.Collections;

public abstract class InGameFlowStateBase : IInGameFlowState
{
    public virtual string StateName => GetType().Name;

    public virtual IEnumerator Enter(InGameFlowContext context)
    {
        yield break;
    }

    public abstract IEnumerator Execute(InGameFlowContext context);

    public virtual IEnumerator Exit(InGameFlowContext context)
    {
        yield break;
    }
}
