using System.Collections;

// Wave終了演出State
public sealed class WaveEndFlowState : WaitSecondsFlowState
{
    // 仮の待機時間設定
    public WaveEndFlowState() : base(1f)
    {
    }

    // Wave終了演出の開始処理
    public override IEnumerator Enter(InGameFlowContext context)
    {
        // Wave end presentation hook.
        yield break;
    }
}
