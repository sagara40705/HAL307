// InGame進行中の一時的な入力・自動処理ロック
public static class InGameFlowLock
{
    public static bool IsGameStartActionPlaying { get; private set; }

    // ゲーム開始演出中のロック開始
    public static void BeginGameStartAction()
    {
        IsGameStartActionPlaying = true;
    }

    // ゲーム開始演出中のロック終了
    public static void EndGameStartAction()
    {
        IsGameStartActionPlaying = false;
    }
}
