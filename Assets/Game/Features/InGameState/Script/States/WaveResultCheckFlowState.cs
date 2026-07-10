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
    private readonly WaveData _waveData;
    private readonly int _currentScore;
    private readonly bool _hasNextWave;

    public WaveResultCheckResult Result { get; private set; }
    public bool IsTargetScoreReached { get; private set; }

    public WaveResultCheckFlowState(WaveData waveData, int currentScore, bool hasNextWave)
    {
        _waveData = waveData;
        _currentScore = currentScore;
        _hasNextWave = hasNextWave;
    }

    // スコアと残りWave数による次の分岐判定
    public override IEnumerator Execute(InGameFlowContext context)
    {
        if (_waveData == null)
        {
            Result = WaveResultCheckResult.GameEnd;
            yield break;
        }

        IsTargetScoreReached = _currentScore >= _waveData.TargetScore;

        if (!IsTargetScoreReached)
        {
            Debug.Log($"[WaveResultCheckFlowState] Target score failed. Score={_currentScore}, Target={_waveData.TargetScore}");
            Result = WaveResultCheckResult.GameEnd;
            yield break;
        }

        Result = _hasNextWave ? WaveResultCheckResult.NextWave : WaveResultCheckResult.GameEnd;
        Debug.Log($"[WaveResultCheckFlowState] Target score reached. Score={_currentScore}, Target={_waveData.TargetScore}, Result={Result}");
        yield break;
    }
}
