using System.Collections;

// ゲーム開始演出State
public sealed class GameStartFlowState : InGameFlowStateBase
{
    private readonly StageData _stageData;

    public bool IsPresentationFinished { get; private set; }

    public GameStartFlowState(StageData stageData)
    {
        _stageData = stageData;
    }

    // ゲーム開始演出の実行
    public override IEnumerator Execute(InGameFlowContext context)
    {
        IsPresentationFinished = false;

        if (_stageData != null)
        {
            if (context == null || context.EnemyLineup == null)
            {
                UnityEngine.Debug.LogWarning("[GameStartFlowState] Enemy setup skipped. Reason=EnemyLineupManager is null.");
            }
            else
            {
                // StageModelと同じゲームモードで敵上限を確定。未初期化時は3P扱い
                GameModeManager gameModeManager = GameModeManager.Instance;
                Enemy.EnemyMaxCount maxEnemyCount = Enemy.EnemyMaxCount.Two;
                if (gameModeManager != null && gameModeManager.IsInitialized)
                {
                    gameModeManager.LockSelection();
                    maxEnemyCount = (Enemy.EnemyMaxCount)gameModeManager.MaxEnemyCount;
                }
                else
                {
                    UnityEngine.Debug.LogWarning("[GameStartFlowState] GameModeManager is not ready. ThreePlayer enemy capacity is used.");
                }

                context.EnemyLineup.SetupInitialEnemies(
                    _stageData.InitialEnemies,
                    _stageData.InitialEnemyCount,
                    maxEnemyCount);
            }
        }

        InGamePresentationAction action = _stageData != null ? _stageData.GameStartAction : null;
        InGameFlowLock.BeginGameStartAction();
        context?.ForwardProgressMover?.StopMove();

        try
        {
            if (action != null)
            {
                yield return action.Play(context);
            }
        }
        finally
        {
            InGameFlowLock.EndGameStartAction();
            IsPresentationFinished = true;
        }
    }
}
