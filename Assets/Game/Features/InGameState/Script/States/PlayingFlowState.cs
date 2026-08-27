using System.Collections;
using UnityEngine;

// StageEndJudgeを待つ通常プレイState
public sealed class PlayingFlowState : InGameFlowStateBase
{
    private readonly StageData _stageData;
    private readonly WaveData _waveData;
    private readonly float _enemyAnimationSpeed;

    public PlayingFlowState(StageData stageData, WaveData waveData, float enemyAnimationSpeed)
    {
        _stageData = stageData;
        _waveData = waveData;
        _enemyAnimationSpeed = Mathf.Max(0f, enemyAnimationSpeed);
    }

    // Wave開始演出の実行
    public override IEnumerator Enter(InGameFlowContext context)
    {
        context?.WaveGauge?.ResetWaveGauge();
        context?.AudienceRequest?.ResetRequests();
        context?.ResetProgressFollowerPositions(_stageData);
        int currentEnemyCount = context?.EnemyLineup?.CurrentEnemyCount ?? 0;
        BeginWaveEnemyReplacement(context);
        ApplyWaveAnimationSpeed(context);


        InGamePresentationAction action = _waveData?.GetWaveStartAction(currentEnemyCount);
        if (action == null)
        {
            CommitPendingEnemyReplacement(context);
            yield return PlayResetPositionAction(context);
            context?.ForwardProgressMover?.ResumeMove();
            yield break;
        }

        InGameFlowLock.BeginPresentation();
        try
        {
            yield return action.Play(context);
        }
        finally
        {
            InGameFlowLock.EndPresentation();
        }

        CommitPendingEnemyReplacement(context);
        yield return PlayResetPositionAction(context);
        context?.ForwardProgressMover?.ResumeMove();
    }

    // StageEndJudgeの終了通知まで待機
    public override IEnumerator Execute(InGameFlowContext context)
    {
        StageEndJudge stageEndJudge = context != null ? context.StageEndJudge : null;
        if (stageEndJudge == null)
        {
            Debug.LogWarning("[PlayingFlowState] StageEndJudge is not assigned.");
            yield break;
        }

        bool isStageEnd = false;

        // ステージ終了通知の受信
        void OnStageEnd()
        {
            isStageEnd = true;
        }

        stageEndJudge.OnStageEnd += OnStageEnd;
        stageEndJudge.ResetJudge();
        try
        {
            while (!isStageEnd)
            {
                yield return null;
            }
        }
        finally
        {
            stageEndJudge.OnStageEnd -= OnStageEnd;
        }
    }

    private void BeginWaveEnemyReplacement(InGameFlowContext context)
    {
        if (_waveData == null || !_waveData.HasReplacementEnemy)
        {
            return;
        }

        Enemy.EnemyLineupManager enemyLineup = context != null ? context.EnemyLineup : null;
        if (enemyLineup == null)
        {
            Debug.LogWarning("[PlayingFlowState] Enemy replacement skipped. Reason=EnemyLineupManager is null.");
            return;
        }

        enemyLineup.BeginWaveEnemyChange(_waveData.ReplacementEnemyData, _waveData.ReplacementEnemyPosition, _waveData.ReplacementEnemyRotation);
    }

    private void ApplyWaveAnimationSpeed(InGameFlowContext context)
    {
        Enemy.EnemyLineupManager enemyLineup = context != null ? context.EnemyLineup : null;
        if (enemyLineup == null)
        {
            Debug.LogWarning("[PlayingFlowState] Animation speed was not applied. Reason=EnemyLineupManager is null.");
            return;
        }

        enemyLineup.SetAllEnemyAnimationSpeed(_enemyAnimationSpeed);
    }

    private void CommitPendingEnemyReplacement(InGameFlowContext context)
    {
        Enemy.EnemyLineupManager enemyLineup = context != null ? context.EnemyLineup : null;
        if (enemyLineup == null || !enemyLineup.HasPendingReplacement)
        {
            return;
        }

        enemyLineup.CommitPendingReplacement();
    }

    private IEnumerator PlayResetPositionAction(InGameFlowContext context)
    {
        InGamePresentationAction resetAction = context != null ? context.ResetPositionAction : null;
        if (resetAction == null)
        {
            yield break;
        }

        InGameFlowLock.BeginPresentation();
        try
        {
            yield return resetAction.Play(context);
        }
        finally
        {
            InGameFlowLock.EndPresentation();
        }
    }
}
