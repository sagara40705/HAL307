using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// LoadingView（共通ローディング表示）
///
/// 目的：
/// - 進捗バーとテキストを表示するだけの最小View
/// - 表示/非表示はCanvasGroupで制御
///
/// 配置：
/// - RootUIシーン内のCanvas配下に置く
/// - RootUIRefs.LoadingView に割り当てる
/// </summary>
public sealed class LoadingView : MonoBehaviour
{
    [Header("Root")]
    [Tooltip("まとめて表示/非表示するCanvasGroup。なければ自動で取得する。")]
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Widgets")]
    [SerializeField] private Slider progressBar;
    [SerializeField] private Text labelText;
    [SerializeField] private Text hintText;

    private void Awake()
    {
        // 参照が未設定でも最低限動くようにする
        if (canvasGroup == null) canvasGroup = GetComponentInChildren<CanvasGroup>(true);

        // 初期状態は非表示にしておく（RootUIManager側でも隠すが保険）
        Hide();
    }

    /// <summary>表示する。</summary>
    public void Show()
    {
        if (canvasGroup == null) { gameObject.SetActive(true); return; }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;  // ロード中は基本操作させない想定
        canvasGroup.blocksRaycasts = true; // クリックを下に通したくない場合true
        gameObject.SetActive(true);
    }

    /// <summary>非表示にする。</summary>
    public void Hide()
    {
        if (canvasGroup == null) { gameObject.SetActive(false); return; }

        canvasGroup.alpha = 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
        gameObject.SetActive(false);
    }

    /// <summary>進捗（0..1）を設定。</summary>
    public void SetProgress(float progress01)
    {
        if (progressBar == null) return;
        progressBar.value = Mathf.Clamp01(progress01);
    }

    /// <summary>ラベルを設定（例：Loading Title... 50%）。</summary>
    public void SetLabel(string targetSceneName, float progress01)
    {
        if (labelText == null) return;

        string scenePart = string.IsNullOrWhiteSpace(targetSceneName) ? "" : $" {targetSceneName}";
        int percent = Mathf.RoundToInt(Mathf.Clamp01(progress01) * 100f);

        labelText.text = $"Loading{scenePart}... {percent}%";
    }

    /// <summary>ヒント表示（例：Press Space）。空文字で消す。</summary>
    public void SetHint(string hint)
    {
        if (hintText == null) return;
        hintText.text = hint ?? string.Empty;
    }
}
