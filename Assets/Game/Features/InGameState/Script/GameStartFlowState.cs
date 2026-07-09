using System.Collections;

// Game start presentation state.
public sealed class GameStartFlowState : InGameFlowStateBase
{
    private readonly StageData _stageData;

    public GameStartFlowState(StageData stageData)
    {
        _stageData = stageData;
    }

    // Plays game start presentation.
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
