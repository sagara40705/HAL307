// InGame進行中の一時的な入力・自動処理ロック
public static class InGameFlowLock
{
    private static int _presentationLockCount;

    public static bool IsPresentationPlaying => _presentationLockCount > 0;
    public static bool IsGameStartActionPlaying { get; private set; }

    public static void BeginPresentation()
    {
        _presentationLockCount++;
    }

    public static void EndPresentation()
    {
        if (_presentationLockCount <= 0)
        {
            _presentationLockCount = 0;
            return;
        }

        _presentationLockCount--;
    }

    // ゲーム開始演出中のロック開始
    public static void BeginGameStartAction()
    {
        IsGameStartActionPlaying = true;
        BeginPresentation();
    }

    // ゲーム開始演出中のロック終了
    public static void EndGameStartAction()
    {
        IsGameStartActionPlaying = false;
        EndPresentation();
    }
}
