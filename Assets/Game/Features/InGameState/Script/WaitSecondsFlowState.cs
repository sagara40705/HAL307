using System.Collections;
using UnityEngine;

// 指定秒数待機するState
public class WaitSecondsFlowState : InGameFlowStateBase
{
    private readonly float _seconds;

    // 待機秒数の設定
    public WaitSecondsFlowState(float seconds)
    {
        _seconds = Mathf.Max(0f, seconds);
    }

    // 指定秒数の待機
    public override IEnumerator Execute(InGameFlowContext context)
    {
        if (_seconds <= 0f)
        {
            yield break;
        }

        yield return new WaitForSeconds(_seconds);
    }
}
