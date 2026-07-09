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

    public override IEnumerator Execute(InGameFlowContext context)
    {
        bool hasReplacement = _nextWave != null && _nextWave.HasReplacementEnemy;
        Enemy.EnemyLineupManager enemyLineup = context != null ? context.EnemyLineup : null;

        if (hasReplacement)
        {
            if (enemyLineup == null)
            {
                UnityEngine.Debug.LogWarning("[WaveTransitionFlowState] Enemy replacement skipped. Reason=EnemyLineupManager is null.");
            }
            else
            {
                enemyLineup.BeginLowestScoreEnemyReplacement(_nextWave.ReplacementEnemyData, _nextWave.ReplacementEnemyPosition, _nextWave.ReplacementEnemyRotation);
            }
        }

        InGamePresentationAction action = _completedWave?.WaveTransitionAction;
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

        if (!hasReplacement || enemyLineup == null)
        {
            yield break;
        }

        enemyLineup.CommitPendingReplacement();
    }
}
