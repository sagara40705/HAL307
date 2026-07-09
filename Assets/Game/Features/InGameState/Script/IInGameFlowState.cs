using System.Collections;

// Common interface for in-game flow states.
public interface IInGameFlowState
{
    // State name for logs.
    string StateName { get; }

    // Logic before the main state body.
    IEnumerator Enter(InGameFlowContext context);

    // Main state body.
    IEnumerator Execute(InGameFlowContext context);

    // Logic after the main state body.
    IEnumerator Exit(InGameFlowContext context);
}
