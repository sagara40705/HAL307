using System.Collections;

// Wave変更演出State
public sealed class WaveTransitionFlowState : InGameFlowStateBase
{
    private readonly WaveData _completedWave;
    private readonly WaveData _nextWave;

    public WaveTransitionFlowState(WaveData completedWave)
        : this(completedWave, null)
    {
    }

    public WaveTransitionFlowState(WaveData completedWave, WaveData nextWave)
    {
        _completedWave = completedWave;
        _nextWave = nextWave;
    }

    // 完了したWaveの変更演出を実行
    public override IEnumerator Execute(InGameFlowContext context)
    {
        InGamePresentationAction action = _completedWave?.WaveTransitionAction;
        if (action != null)
        {
            yield return action.Play(context);
        }

        if (_nextWave == null || !_nextWave.HasReplacementEnemy)
        {
            yield break;
        }

        if (context == null || context.EnemyLineup == null)
        {
            UnityEngine.Debug.LogWarning("[WaveTransitionFlowState] Enemy replacement skipped. Reason=EnemyLineupManager is null.");
            yield break;
        }

        context.EnemyLineup.ReplaceLowestScoreEnemy(_nextWave.ReplacementEnemyData, _nextWave.ReplacementEnemyPosition, _nextWave.ReplacementEnemyRotation);
    }
}
