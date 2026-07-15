using System;
using Context;
using UnityEngine;

// Result Scene内でゲーム結果のスナップショットを保持するContext
public sealed class ResultSceneContext : ContextBase
{
   

    [SerializeField] private int clearedWaveCount;
    [SerializeField] private int parrySuccessCount;
    [SerializeField] private int score;
    [SerializeField] private bool hasCompletedResult;

    public int ClearedWaveCount => clearedWaveCount;
    public int ParrySuccessCount => parrySuccessCount;
    public int Score => score;
    public bool HasCompletedResult => hasCompletedResult;

    public event Action ResultCaptured;

    protected override void Awake()
    {
        base.Awake();
        CaptureFromManager();
    }

    // 常駐Managerの現在値をSceneローカルの値として保存する
    public void CaptureFromManager()
    {
        GameResultManager manager = GameResultManager.Instance;
        if (manager == null)
        {
            clearedWaveCount = 0;
            parrySuccessCount = 0;
            score = 0;
            hasCompletedResult = false;
            Debug.LogWarning("[ResultSceneContext] Result capture failed. Reason=GameResultManager is not available.");
            ResultCaptured?.Invoke();
            return;
        }


        clearedWaveCount = manager.ClearedWaveCount;
        parrySuccessCount = manager.ParrySuccessCount;
        score = manager.Score;
        hasCompletedResult = manager.HasCompletedResult;
        ResultCaptured?.Invoke();
    }
}
