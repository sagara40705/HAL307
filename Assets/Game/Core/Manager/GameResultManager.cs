using System;
using System.Collections;
using UnityEngine;

// InGameの集計結果をResult Sceneまで保持する常駐Manager
public sealed class GameResultManager : AppManagerBase<GameResultManager>
{
    public int ClearedWaveCount { get; private set; }
    public int ParrySuccessCount { get; private set; }
    public int Score { get; private set; }
    public bool HasCompletedResult { get; private set; }

    public event Action ResultChanged;

    protected override IEnumerator OnInitialize()
    {
        ResetResult();
        yield break;
    }

    // 新しいInGame開始時の集計初期化
    public void BeginRun()
    {
        ResetResult();
    }

    // 到達済みの最大Wave数を記録
    public void RecordWaveCleared(int clearedWaveCount)
    {
        int nextCount = Mathf.Max(0, clearedWaveCount);
        if (nextCount <= ClearedWaveCount)
        {
            return;
        }

        ClearedWaveCount = nextCount;
        ResultChanged?.Invoke();
    }

    // パリィ成功回数を加算
    public void RecordParrySuccess()
    {
        ParrySuccessCount++;
        ResultChanged?.Invoke();
    }

    // Result遷移前に最終スコアを確定
    public void CompleteRun(int score)
    {
        Score = Mathf.Max(0, score);
        HasCompletedResult = true;
        ResultChanged?.Invoke();
    }

    private void ResetResult()
    {
        ClearedWaveCount = 0;
        ParrySuccessCount = 0;
        Score = 0;
        HasCompletedResult = false;
        ResultChanged?.Invoke();
    }
}
