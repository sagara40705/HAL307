using System.Collections;

// ゲーム終了演出とリザルト遷移State
public sealed class GameEndFlowState : InGameFlowStateBase
{
    private readonly StageData _stageData;

    public GameEndFlowState(StageData stageData)
    {
        _stageData = stageData;
    }

    // ゲーム終了演出の実行とリザルトシーン読み込み
    public override IEnumerator Execute(InGameFlowContext context)
    {
        InGamePresentationAction action = _stageData != null ? _stageData.GameEndAction : null;
        if (action != null)
        {
            InGameFlowLock.BeginPresentation();
            try
            {
                yield return action.Play(context);
            }
            finally
            {
                InGameFlowLock.EndPresentation();
            }
        }

        string resultSceneName = _stageData != null ? _stageData.ResultSceneName : string.Empty;
        if (!string.IsNullOrWhiteSpace(resultSceneName))
        {
            SceneLoader.Load(resultSceneName);
        }
    }
}
