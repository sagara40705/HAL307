using System.Collections;

// Wave開始演出State
public sealed class WaveIntroFlowState : WaitSecondsFlowState
{
    // 仮の待機時間設定
    public WaveIntroFlowState() : base(1f)
    {
    }

    // Wave開始演出の開始処理
    public override IEnumerator Enter(InGameFlowContext context)
    {
        // Wave intro presentation hook.
        yield break;
    }
}
