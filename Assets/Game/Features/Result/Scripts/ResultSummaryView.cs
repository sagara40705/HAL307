using TMPro;
using UnityEngine;

// GameResultManagerの集計値をResult UIへ反映するView
public sealed class ResultSummaryView : MonoBehaviour
{
    [SerializeField] private TMP_Text clearedWaveCountText;
    [SerializeField] private TMP_Text parrySuccessCountText;
    [SerializeField] private TMP_Text scoreText;

    private void OnEnable()
    {
        GameResultManager manager = GameResultManager.Instance;
        if (manager != null)
        {
            manager.ResultChanged += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        GameResultManager manager = GameResultManager.Instance;
        if (manager != null)
        {
            manager.ResultChanged -= Refresh;
        }
    }

    // 現在保持されている集計値をTextへ反映
    public void Refresh()
    {
        GameResultManager manager = GameResultManager.Instance;
        int clearedWaveCount = manager != null ? manager.ClearedWaveCount : 0;
        int parrySuccessCount = manager != null ? manager.ParrySuccessCount : 0;
        int score = manager != null ? manager.Score : 0;

        SetText(clearedWaveCountText, clearedWaveCount);
        SetText(parrySuccessCountText, parrySuccessCount);
        SetText(scoreText, score);
    }

    private static void SetText(TMP_Text target, int value)
    {
        if (target != null)
        {
            target.text = value.ToString();
        }
    }
}
