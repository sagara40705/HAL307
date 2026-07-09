using UnityEngine;

// Scene references used by in-game flow states.
public sealed class InGameFlowContext : MonoBehaviour
{
    [SerializeField] private ForwardProgressMover forwardProgressMover;
    [SerializeField] private StageEndJudge stageEndJudge;
    [SerializeField] private ScoreManager scoreManager;

    // Forward movement controller.
    public ForwardProgressMover ForwardProgressMover => forwardProgressMover;

    // Stage end trigger.
    public StageEndJudge StageEndJudge => stageEndJudge;

    // Score source.
    public ScoreManager ScoreSource
    {
        get
        {
            ResolveScoreManager();
            return scoreManager;
        }
    }

    // Current player score.
    public int CurrentPlayerScore => ScoreSource != null ? ScoreSource.PlayerScore : 0;

    private void Awake()
    {
        ResolveScoreManager();
    }

    // Finds ScoreManager when inspector reference is empty.
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
}
