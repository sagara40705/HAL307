using System.Collections;

// ゲーム開始演出State
public sealed class GameStartFlowState : InGameFlowStateBase
{
    private readonly StageData _stageData;

    public GameStartFlowState(StageData stageData)
    {
        _stageData = stageData;
    }

    // ゲーム開始演出の実行
    public override IEnumerator Execute(InGameFlowContext context)
    {
        InGamePresentationAction action = _stageData != null ? _stageData.GameStartAction : null;
        if (action == null)
        {
            yield break;
        }

        yield return action.Play(context);
    }
}
