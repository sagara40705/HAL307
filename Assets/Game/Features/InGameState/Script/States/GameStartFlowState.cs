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
        if (_stageData != null && _stageData.HasInitialEnemies)
        {
            if (context == null || context.EnemyLineup == null)
            {
                UnityEngine.Debug.LogWarning("[GameStartFlowState] Enemy setup skipped. Reason=EnemyLineupManager is null.");
            }
            else
            {
                context.EnemyLineup.SetupInitialEnemies(_stageData.InitialEnemies);
            }
        }

        InGamePresentationAction action = _stageData != null ? _stageData.GameStartAction : null;
        if (action == null)
        {
            yield break;
        }

        yield return action.Play(context);
    }
}
