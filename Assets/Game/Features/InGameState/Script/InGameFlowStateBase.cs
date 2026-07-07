using System.Collections;

// インゲーム進行Stateの基底クラス
public abstract class InGameFlowStateBase : IInGameFlowState
{
    // クラス名を標準のState名にする
    public virtual string StateName => GetType().Name;

    // State開始時の標準処理
    public virtual IEnumerator Enter(InGameFlowContext context)
    {
        yield break;
    }

    // State中のメイン処理
    public abstract IEnumerator Execute(InGameFlowContext context);

    // State終了時の標準処理
    public virtual IEnumerator Exit(InGameFlowContext context)
    {
        yield break;
    }
}
