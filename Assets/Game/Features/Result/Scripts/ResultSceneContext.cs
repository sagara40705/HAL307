using System;
using System.Collections.Generic;
using Context;
using UnityEngine;
using UnityEngine.UI;

// Result Scene内でゲーム結果のスナップショットを保持するContext
public sealed class ResultSceneContext : ContextBase
{
    [Header("数字スプライト（0〜9）を順番に設定")]
    public Sprite[] numberSprites;

    [Header("数字の表示先（右から左へ、1の位から順番に設定）")]
    public Transform[] ParryParent;
    public Transform[] WaveParent;
    public Transform[] ScoreParent;

    [Header("最終点数の表示領域（7桁まで収まる大きさ）")]
    [SerializeField] private RectTransform scoreArea;
    [SerializeField, Min(0)] private float scoreDigitSpacing = 2f;

    private readonly List<Image> waveDigits = new List<Image>();
    private readonly List<Image> parryDigits = new List<Image>();
    private readonly List<Image> scoreDigits = new List<Image>();

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

    // 常駐Managerの現在値をSceneローカルの値として保存する。
    public void CaptureFromManager()
    {
        GameResultManager manager = GameResultManager.Instance;
        clearedWaveCount = manager != null ? manager.ClearedWaveCount : 0;
        parrySuccessCount = manager != null ? manager.ParrySuccessCount : 0;
        score = manager != null ? manager.Score : 0;
        hasCompletedResult = manager != null && manager.HasCompletedResult;

        RefreshDisplay();
        ResultCaptured?.Invoke();
    }

    private void RefreshDisplay()
    {
        if (numberSprites == null || numberSprites.Length < 10)
        {
            return;
        }

        DisplayNumber(clearedWaveCount, WaveParent, waveDigits);
        DisplayNumber(parrySuccessCount, ParryParent, parryDigits);
        DisplayNumber(score, ScoreParent, scoreDigits);
        LayoutScoreDigits();
    }

    private void DisplayNumber(int value, Transform[] parents, List<Image> digits)
    {
        if (parents == null || parents.Length == 0)
        {
            return;
        }

        if (digits.Count == 0)
        {
            // 表示先に既にあるImageを使い、再取得のたびに数字が重ならないようにする。
            foreach (Transform parent in parents)
            {
                if (parent == null || !parent.TryGetComponent(out Image digit))
                {
                    digits.Clear();
                    Debug.LogWarning("[ResultSceneContext] 数字の表示先にImageを設定してください。", this);
                    return;
                }

                digits.Add(digit);
            }
        }

        string text = Mathf.Max(0, value).ToString();
        // エンドレス等で用意した桁数を超えても、上位桁を切り捨てず左へ追加する。
        while (digits.Count < text.Length)
        {
            RectTransform rightmost = digits[0].rectTransform;
            RectTransform leftmost = digits[digits.Count - 1].rectTransform;
            var digitObject = new GameObject("Digit", typeof(RectTransform), typeof(Image));
            Image digit = digitObject.GetComponent<Image>();
            RectTransform rect = digit.rectTransform;
            rect.SetParent(rightmost, false);
            rect.anchorMin = rightmost.pivot;
            rect.anchorMax = rightmost.pivot;
            rect.pivot = rightmost.pivot;
            rect.sizeDelta = rightmost.sizeDelta;
            float leftEdge = digits.Count == 1 ? 0f : leftmost.anchoredPosition.x;
            rect.anchoredPosition = new Vector2(leftEdge - rightmost.rect.width, 0f);
            digits.Add(digit);
        }

        for (int i = 0; i < digits.Count; i++)
        {
            Image digit = digits[i];
            // Image.enabledだけでは、非アクティブな親の下にある桁は表示されない。
            digit.gameObject.SetActive(true);
            digit.enabled = i < text.Length;
            digit.raycastTarget = false;
            if (!digit.enabled)
            {
                continue;
            }

            // 配列の先頭は1の位なので、文字列は末尾から読む。
            int number = text[text.Length - 1 - i] - '0';
            digit.sprite = numberSprites[number];
            digit.preserveAspect = true;
        }
    }

    private void LayoutScoreDigits()
    {
        if (scoreArea == null || scoreDigits.Count == 0)
        {
            return;
        }

        int count = Mathf.Max(0, score).ToString().Length;
        float height = scoreArea.rect.height;
        float width = scoreDigitSpacing * (count - 1);
        for (int i = 0; i < count; i++)
        {
            Rect spriteRect = scoreDigits[i].sprite.rect;
            width += height * spriteRect.width / spriteRect.height;
        }

        // 全ての数字を同じ高さに揃え、桁数が増えた場合は列全体を等倍で縮める。
        // 「最終点数」のラベルやPtへはみ出さず、値を切り捨てずに右揃えで表示する。
        float scale = width > 0f ? Mathf.Min(1f, scoreArea.rect.width / width) : 1f;
        float right = scoreArea.rect.xMax;
        for (int i = 0; i < scoreDigits.Count; i++)
        {
            RectTransform rect = scoreDigits[i].rectTransform;
            rect.SetParent(scoreArea, false);
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = rect.anchorMin;
            rect.pivot = new Vector2(1f, 0.5f);
            rect.localScale = Vector3.one;
            if (i >= count)
            {
                continue;
            }

            Rect spriteRect = scoreDigits[i].sprite.rect;
            float digitWidth = height * spriteRect.width / spriteRect.height * scale;
            rect.sizeDelta = new Vector2(digitWidth, height * scale);
            rect.anchoredPosition = new Vector2(right - scoreArea.rect.xMax, 0f);
            right -= digitWidth + scoreDigitSpacing * scale;
        }
    }
}
