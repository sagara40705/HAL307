using System.Collections;
using UnityEngine;

// Presentation action data executed by in-game flow states.
public abstract class InGamePresentationAction : ScriptableObject
{
    // Executes presentation logic.
    public abstract IEnumerator Play(InGameFlowContext context);
}
