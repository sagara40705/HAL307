using System.Collections;

// インゲーム進行Stateの共通インターフェース
public interface IInGameFlowState
{
    // ログ表示用のState名
    string StateName { get; }

    // State開始時の処理
    IEnumerator Enter(InGameFlowContext context);

    // State中のメイン処理
    IEnumerator Execute(InGameFlowContext context);

    // State終了時の処理
    IEnumerator Exit(InGameFlowContext context);
}
