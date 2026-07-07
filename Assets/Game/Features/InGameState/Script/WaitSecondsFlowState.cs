using System.Collections;
using UnityEngine;

public class WaitSecondsFlowState : InGameFlowStateBase
{
    private readonly float _seconds;

    public WaitSecondsFlowState(float seconds)
    {
        _seconds = Mathf.Max(0f, seconds);
    }

    public override IEnumerator Execute(InGameFlowContext context)
    {
        if (_seconds <= 0f)
        {
            yield break;
        }

        yield return new WaitForSeconds(_seconds);
    }
}
