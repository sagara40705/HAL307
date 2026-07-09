using System.Collections;

// Game end presentation and result transition state.
public sealed class GameEndFlowState : InGameFlowStateBase
{
    private readonly StageData _stageData;

    public GameEndFlowState(StageData stageData)
    {
        _stageData = stageData;
    }

    // Plays game end presentation and loads result scene.
    public override IEnumerator Execute(InGameFlowContext context)
    {
        InGamePresentationAction action = _stageData != null ? _stageData.GameEndAction : null;
        if (action != null)
        {
            yield return action.Play(context);
        }

        string resultSceneName = _stageData != null ? _stageData.ResultSceneName : string.Empty;
        if (!string.IsNullOrWhiteSpace(resultSceneName))
        {
            SceneLoader.Load(resultSceneName);
        }
    }
}
