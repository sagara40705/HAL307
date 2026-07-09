using Enemy;
using UnityEngine;

// インゲーム進行Stateから参照するシーン要素
public sealed class InGameFlowContext : MonoBehaviour
{
    [SerializeField] private string stageModelContextName = "StageModelContext";
    [SerializeField] private StageModelContext stageModelContext;
    [SerializeField] private ForwardProgressMover forwardProgressMover;
    [SerializeField] private StageEndJudge stageEndJudge;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private EnemyLineupManager enemyLineupManager;

    // 前進制御
    public ForwardProgressMover ForwardProgressMover
    {
        get
        {
            ResolveForwardProgressMover();
            return forwardProgressMover;
        }
    }

    // ステージ終了判定
    public StageEndJudge StageEndJudge
    {
        get
        {
            ResolveStageModelContext();
            return stageEndJudge != null ? stageEndJudge : stageModelContext?.StageEndJudge;
        }
    }

    // ステージ終了判定の位置
    public Vector3 StageEndJudgePosition
    {
        get
        {
            ResolveStageModelContext();
            return stageEndJudge != null ? stageEndJudge.transform.position : stageModelContext != null ? stageModelContext.StageEndJudgePosition : Vector3.zero;
        }
    }

    // Wave終了エリアの位置
    public Vector3 WaveEndAreaPosition
    {
        get
        {
            ResolveStageModelContext();
            return stageModelContext != null ? stageModelContext.WaveEndAreaPosition : Vector3.zero;
        }
    }

    // スコア参照元
    public ScoreManager ScoreSource
    {
        get
        {
            ResolveScoreManager();
            return scoreManager;
        }
    }

    // 現在のプレイヤースコア
    public int CurrentPlayerScore => ScoreSource != null ? ScoreSource.PlayerScore : 0;

    public EnemyLineupManager EnemyLineup
    {
        get
        {
            ResolveEnemyLineupManager();
            return enemyLineupManager;
        }
    }

    private void Awake()
    {
        ResolveStageModelContext();
        ResolveForwardProgressMover();
        ResolveScoreManager();
        ResolveEnemyLineupManager();
    }

    // Inspector未設定時のForwardProgressMover検索
    private void ResolveForwardProgressMover()
    {
        if (forwardProgressMover != null)
        {
            return;
        }

        forwardProgressMover = FindFirstObjectByType<ForwardProgressMover>();
    }

    // Inspector未設定時のStageModelContext検索
    private void ResolveStageModelContext()
    {
        if (stageModelContext != null)
        {
            return;
        }

        ContextManager manager = ContextManager.Instance;
        if (manager != null && !string.IsNullOrWhiteSpace(stageModelContextName))
        {
            stageModelContext = manager.Get<StageModelContext>(stageModelContextName);
        }

        if (stageModelContext == null)
        {
            stageModelContext = FindFirstObjectByType<StageModelContext>();
        }
    }

    // Inspector未設定時のScoreManager検索
    private void ResolveScoreManager()
    {
        if (scoreManager != null)
        {
            return;
        }

        scoreManager = global::ScoreManager.Instance;
        if (scoreManager == null)
        {
            scoreManager = FindFirstObjectByType<ScoreManager>();
        }
    }

    // Inspector未設定時のEnemyLineupManager検索
    private void ResolveEnemyLineupManager()
    {
        if (enemyLineupManager != null)
        {
            return;
        }

        enemyLineupManager = FindFirstObjectByType<EnemyLineupManager>();
    }
}
