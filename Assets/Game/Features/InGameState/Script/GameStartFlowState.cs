using System.Collections;

// ゲーム開始演出State
public sealed class GameStartFlowState : WaitSecondsFlowState
{
    // 仮の待機時間設定
    public GameStartFlowState() : base(1f)
    {
    }

    // ゲーム開始演出の開始処理
    public override IEnumerator Enter(InGameFlowContext context)
    {
        // Game start presentation hook.
        yield break;
    }
}
