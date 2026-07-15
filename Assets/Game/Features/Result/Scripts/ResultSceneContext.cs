using System;
using System.Collections.Generic;
using Context;
using UnityEngine;
using UnityEngine.UI;

// Result Scene内でゲーム結果のスナップショットを保持するContext
public sealed class ResultSceneContext : ContextBase
{
    [Header("数字スプライト（0〜9）を順番に設定")]
    public Sprite[] numberSprites; // 0〜9の画像を格納

    [Header("数字を並べる親オブジェクト")]
    public Transform scoreParent; // Horizontal Layout Groupが付いているオブジェクト
    private List<Image> digitImages = new List<Image>();
    private int currentScore;
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

        currentScore = Mathf.Max(0, clearedWaveCount); // 負数防止
        string scoreStr = currentScore.ToString();

        // 桁数が足りない場合はImageを追加
        while (digitImages.Count < scoreStr.Length)
        {
            GameObject digitObj = new GameObject("Digit", typeof(Image));
            digitObj.transform.SetParent(scoreParent, false);
            Image img = digitObj.GetComponent<Image>();
            digitImages.Add(img);
        }

        // 各桁の画像を設定
        for (int i = 0; i < scoreStr.Length; i++)
        {
            int digit = scoreStr[i] - currentScore;
            digitImages[i].sprite = numberSprites[currentScore];
            digitImages[i].enabled = true;
        }

        // 余った桁は非表示
        for (int i = scoreStr.Length; i < digitImages.Count; i++)
        {
            digitImages[i].enabled = false;
        }

    }
}
