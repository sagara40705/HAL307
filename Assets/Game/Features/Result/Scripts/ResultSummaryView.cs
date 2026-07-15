using TMPro;
using UnityEngine;

// GameResultManagerの集計値をResult UIへ反映するView
public sealed class ResultSummaryView : MonoBehaviour
{
    [SerializeField] private ResultSceneContext resultContext;
    [SerializeField] private TMP_Text clearedWaveCountText;
    [SerializeField] private TMP_Text parrySuccessCountText;
    [SerializeField] private TMP_Text scoreText;

    private void OnEnable()
    {
        ResolveContext();
        if (resultContext != null)
        {
            resultContext.ResultCaptured += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (resultContext != null)
        {
            resultContext.ResultCaptured -= Refresh;
        }
    }

    // 現在保持されている集計値をTextへ反映
    public void Refresh()
    {
        ResolveContext();
        int clearedWaveCount = resultContext != null ? resultContext.ClearedWaveCount : 0;
        int parrySuccessCount = resultContext != null ? resultContext.ParrySuccessCount : 0;
        int score = resultContext != null ? resultContext.Score : 0;

        SetText(clearedWaveCountText, clearedWaveCount);
        SetText(parrySuccessCountText, parrySuccessCount);
        SetText(scoreText, score);
    }

    private void ResolveContext()
    {
        if (resultContext == null)
        {
            resultContext = FindFirstObjectByType<ResultSceneContext>();
        }
    }

    private static void SetText(TMP_Text target, int value)
    {
        if (target != null)
        {
            target.text = value.ToString();
        }
    }
}
