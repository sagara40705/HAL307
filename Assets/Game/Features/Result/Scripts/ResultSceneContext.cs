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
    public Transform[] ParryParent; // Horizontal Layout Groupが付いているオブジェクト
    public Transform[] WaveParent;
    public Transform[] ScoreParent;
    private List<Image> digitImages = new List<Image>();
    private List<Image> digitImages1 = new List<Image>();
    private List<Image> digitImages2 = new List<Image>();
    private int currentScore;
    private int ParrySocre;
    private int MaxPoint;
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
        //if (manager == null)
        //{
        //    clearedWaveCount = 0;
        //    parrySuccessCount = 0;
        //    score = 0;
        //    hasCompletedResult = false;
        //    Debug.LogWarning("[ResultSceneContext] Result capture failed. Reason=GameResultManager is not available.");
        //    ResultCaptured?.Invoke();
        //    return;
        //}

      
        clearedWaveCount = manager.ClearedWaveCount;
        parrySuccessCount = manager.ParrySuccessCount;
        score = manager.Score;
        hasCompletedResult = manager.HasCompletedResult;
        ResultCaptured?.Invoke();
        //親オブジェクトの数分回す
        currentScore = Mathf.Max(0, clearedWaveCount); // 負数防止
        ParrySocre= Mathf.Max(0, parrySuccessCount);
        MaxPoint= Mathf.Max(0, score);
        string WaveStr = currentScore.ToString();
        string ParryStr = ParrySocre.ToString();
        string ScoreStr = MaxPoint.ToString();
        int Wave = WaveParent.Length;
        int Parry =ParryParent.Length;
        int Score= ScoreParent.Length;
        // 桁数が足りない場合はImageを追加
        for (int i=0; i <= WaveStr.Length;i++)
        {
            GameObject digitObj = new GameObject("Digit", typeof(Image));
            digitObj.transform.SetParent(WaveParent[i], false);
            Image img = digitObj.GetComponent<Image>();
            digitImages.Add(img);
        }
        for (int u = 0; u <= ParryStr.Length; u++)
        {
            GameObject digitObj1 = new GameObject("Digit", typeof(Image));
            digitObj1.transform.SetParent(ParryParent[u], false);
            Image img1 = digitObj1.GetComponent<Image>();
            digitImages1.Add(img1);
        }
        for (int h = 0; h <= ScoreStr.Length; h++)
        {
            GameObject digitObj2 = new GameObject("Digit", typeof(Image));
            digitObj2.transform.SetParent(ScoreParent[h], false);
            Image img2 = digitObj2.GetComponent<Image>();
            digitImages2.Add(img2);
        }

        // 各桁の画像を設定
        for (int i = 0; i < ScoreStr.Length; i++)
        {
            int digit = ScoreStr[i] - '0';
            digitImages[i].sprite = numberSprites[digit];
            digitImages[i].preserveAspect = true;
            digitImages[i].enabled = true;
        }
        for (int i = 0; i < ParryStr.Length; i++)
        {
            int digit = ParryStr[i] - '0';
            digitImages1[i].sprite = numberSprites[digit];
            digitImages1[i].preserveAspect = true;
            digitImages1[i].enabled = true;
        }
        for (int i = 0; i < WaveStr.Length; i++)
        {
            int digit = WaveStr[i] - '0';
            digitImages2[i].sprite = numberSprites[digit];
            digitImages2[i].preserveAspect = true;
            digitImages2[i].enabled = true;
        }

        // 余った桁は非表示
        for (int i = ScoreStr.Length; i < digitImages.Count; i++)
        {
            digitImages[i].enabled = false;
            digitImages1[i].enabled = false;
            digitImages2[i].enabled = false;
        }

    }
}
