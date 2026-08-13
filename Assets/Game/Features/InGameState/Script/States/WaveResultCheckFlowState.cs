using System.Collections;
using UnityEngine;

public enum WaveResultCheckResult
{
    None,
    NextWave,
    GameEnd,
}

// StageEndJudge後のスコア判定State
public sealed class WaveResultCheckFlowState : InGameFlowStateBase
{
    private readonly int _targetScore;
    private readonly int _currentScore;
    private readonly bool _hasNextWave;

    public WaveResultCheckResult Result { get; private set; }
    public bool IsTargetScoreReached { get; private set; }

    public WaveResultCheckFlowState(int targetScore, int currentScore, bool hasNextWave)
    {
        _targetScore = Mathf.Max(0, targetScore);
        _currentScore = currentScore;
        _hasNextWave = hasNextWave;
    }

    // スコアと残りWave数による次の分岐判定
    public override IEnumerator Execute(InGameFlowContext context)
    {
        IsTargetScoreReached = _currentScore >= _targetScore;

        if (!IsTargetScoreReached)
        {
            Debug.Log($"[WaveResultCheckFlowState] Target score failed. Score={_currentScore}, Target={_targetScore}");
            Result = WaveResultCheckResult.GameEnd;
            yield break;
        }

        Result = _hasNextWave ? WaveResultCheckResult.NextWave : WaveResultCheckResult.GameEnd;
        Debug.Log($"[WaveResultCheckFlowState] Target score reached. Score={_currentScore}, Target={_targetScore}, Result={Result}");
        yield break;
    }
}
