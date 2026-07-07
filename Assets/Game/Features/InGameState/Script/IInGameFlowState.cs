using System.Collections;

public interface IInGameFlowState
{
    string StateName { get; }

    IEnumerator Enter(InGameFlowContext context);
    IEnumerator Execute(InGameFlowContext context);
    IEnumerator Exit(InGameFlowContext context);
}
