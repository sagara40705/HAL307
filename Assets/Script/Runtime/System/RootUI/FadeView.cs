using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// FadeView：全画面フェード（最小）
/// — CanvasGroupでalphaを0..1にするだけ
/// </summary>
public sealed class FadeView : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;

    private Coroutine _co;

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponentInChildren<CanvasGroup>(true);

        HideImmediate();
    }

    public void HideImmediate()
    {
        if (canvasGroup == null) return;
        if (_co != null) { StopCoroutine(_co); _co = null; }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    public void FadeOut(float seconds)
    {
        if (canvasGroup == null) return;
        gameObject.SetActive(true);
        StartFade(1f, seconds, blockRaycasts: true);
    }

    public void FadeIn(float seconds)
    {
        if (canvasGroup == null) return;
        gameObject.SetActive(true);
        StartFade(0f, seconds, blockRaycasts: false);
    }

    private void StartFade(float targetAlpha, float seconds, bool blockRaycasts)
    {
        if (_co != null) StopCoroutine(_co);
        _co = StartCoroutine(FadeRoutine(targetAlpha, seconds, blockRaycasts));
    }

    private IEnumerator FadeRoutine(float targetAlpha, float seconds, bool blockRaycasts)
    {
        canvasGroup.blocksRaycasts = blockRaycasts;

        float start = canvasGroup.alpha;
        if (seconds <= 0f)
        {
            canvasGroup.alpha = targetAlpha;
            if (Mathf.Approximately(targetAlpha, 0f)) gameObject.SetActive(false);
            _co = null;
            yield break;
        }

        float t = 0f;
        while (t < seconds)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / seconds);
            canvasGroup.alpha = Mathf.Lerp(start, targetAlpha, a);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        if (Mathf.Approximately(targetAlpha, 0f)) gameObject.SetActive(false);
        _co = null;
    }
}
