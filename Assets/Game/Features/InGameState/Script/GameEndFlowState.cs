using System.Collections;

// ゲーム終了演出State
public sealed class GameEndFlowState : WaitSecondsFlowState
{
    // 仮の待機時間設定
    public GameEndFlowState() : base(1f)
    {
    }

    // ゲーム終了演出またはリザルト遷移の開始処理
    public override IEnumerator Enter(InGameFlowContext context)
    {
        // Game end presentation or result transition hook.
        yield break;
    }
}
