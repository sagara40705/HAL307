using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// 実際のリザルトシーンをプレビューで開き、数字の並びと表示状態を検証する。
public static class ResultDisplayValidation
{
    private const string ScenePath = "Assets/_Shared/ushi/Scenes/Result.unity";
    private static int checks;

    [MenuItem("Tools/Validation/Result Display")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Run validation in Edit Mode.");
        }

        checks = 0;
        var scene = EditorSceneManager.OpenPreviewScene(ScenePath);
        try
        {
            ResultSceneContext[] contexts = scene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<ResultSceneContext>(true)).ToArray();
            Check(contexts.Length == 1, "ResultSceneContext must be unique");
            ResultSceneContext context = contexts[0];
            // プレビューではOverlay Canvasの描画更新が走らないため、座標検証用に倍率を設定する。
            foreach (Canvas canvas in scene.GetRootGameObjects()
                         .SelectMany(root => root.GetComponentsInChildren<Canvas>(true)))
            {
                canvas.transform.localScale = Vector3.one;
            }
            Check(context.numberSprites.Length == 10 && context.numberSprites.All(sprite => sprite != null),
                "All number sprites must be assigned");
            ValidateRow(context.WaveParent);
            ValidateRow(context.ParryParent);
            Check(context.ScoreParent.Length == 7, "Prepare seven score digit slots");
            ValidateRow(context.ScoreParent, context.ScoreParent[0].parent);
            ValidateRatingBannerClip(scene.GetRootGameObjects());

            // 以前は非表示だった桁も、更新時に復帰することを確かめる。
            context.ScoreParent[1].gameObject.SetActive(false);
            context.ScoreParent[2].gameObject.SetActive(false);
            context.ScoreParent[3].gameObject.SetActive(false);
            Show(context, 12, 34, 1234);
            CheckRow(context, context.WaveParent, "12");
            CheckRow(context, context.ParryParent, "34");
            CheckRow(context, context.ScoreParent, "1234");

            foreach (int score in new[] { 1000, 1999, 3000, 3900, 99999, 100000,
                         1111111, 1234567, 4444444, 9999999, int.MaxValue, 0, -1 })
            {
                Show(context, 100, 123, score);
                CheckRow(context, context.WaveParent, "100");
                CheckRow(context, context.ParryParent, "123");
                CheckRow(context, context.ScoreParent, Mathf.Max(0, score).ToString());
                CheckScoreBounds(context);
            }

            int imageCount = CountImages(context);
            Show(context, 1, 2, 1234);
            Show(context, 1, 2, 1234);
            Check(CountImages(context) == imageCount, "Repeated refresh must reuse digits");
            CheckRow(context, context.WaveParent, "1");
            CheckRow(context, context.ParryParent, "2");
            CheckRow(context, context.ScoreParent, "1234");

            // 登場演出の終了後にも、各桁が別々の位置に残っていることを確認する。
            foreach (Transform[] row in new[] { context.WaveParent, context.ParryParent, context.ScoreParent })
            {
                Animator animator = RowRoot(context, row).GetComponent<Animator>();
                AnimationClip clip = animator.runtimeAnimatorController.animationClips[0];
                clip.SampleAnimation(animator.gameObject, clip.length);
            }
            CheckRow(context, context.WaveParent, "1");
            CheckRow(context, context.ParryParent, "2");
            CheckRow(context, context.ScoreParent, "1234");
            CheckScoreBounds(context);

            if (GameResultManager.Instance == null)
            {
                context.CaptureFromManager();
                Check(!context.HasCompletedResult && context.Score == 0, "Missing manager clears the snapshot");
                CheckRow(context, context.ScoreParent, "0");
            }

            Debug.Log($"[ResultDisplayValidation] PASS: {checks} checks.");
        }
        finally
        {
            EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    private static void ValidateRatingBannerClip(GameObject[] roots)
    {
        RectMask2D mask = roots.SelectMany(root => root.GetComponentsInChildren<RectMask2D>(true))
            .Single(item => item.name == "ResultMessageMask");
        Image banner = mask.GetComponentInChildren<Image>();
        Image panels = roots.SelectMany(root => root.GetComponentsInChildren<Image>(true))
            .Single(item => item.name == "ResultItem");
        Check(panels.isActiveAndEnabled && panels.GetComponentInParent<RectMask2D>() == null,
            "Score panels are drawn separately from the rating banner");
        Vector3 originalScale = mask.transform.localScale;
        foreach (Vector3 scale in new[] { Vector3.one, new Vector3(1.24236f, 1.23f, 1f) })
        {
            mask.transform.localScale = scale;
            Rect clip = Clipping.FindCullAndClipWorldRect(
                new System.Collections.Generic.List<RectMask2D> { mask }, out bool valid);
            Check(valid, "The message mask must have a visible area");
            // 評価画像にはロゴ・点数欄・外枠も含まれるため、下部のバナーだけを表示する。
            // 870pxより上の点数欄や左右の枠が、拡大時にも重複して描画されないことを確認する。
            Vector2 CanvasPoint(float sourceY, float sourceX = 960)
            {
                Rect rect = banner.rectTransform.rect;
                Vector3 local = new Vector3(rect.xMin + sourceX / 1920f * rect.width,
                    rect.yMax - sourceY / 1080f * rect.height, 0);
                return mask.GetComponentInParent<Canvas>().transform.InverseTransformPoint(
                    banner.transform.TransformPoint(local));
            }
            Check(!clip.Contains(CanvasPoint(335)), "Hide the static Japanese subtitle completely");
            Check(!clip.Contains(CanvasPoint(349)), "Hide the duplicate wave panel");
            Check(!clip.Contains(CanvasPoint(580)), "Hide the duplicate parry panel");
            Check(!clip.Contains(CanvasPoint(861)), "Hide the duplicate final score panel");
            Check(!clip.Contains(CanvasPoint(1000, 20)), "Hide the duplicate left border");
            Check(!clip.Contains(CanvasPoint(1000, 1900)), "Hide the duplicate right border");
            Check(clip.Contains(CanvasPoint(900)), "Keep the top of the rating banner visible");
            Check(clip.Contains(CanvasPoint(1000)), "Keep the rating banner visible");
        }
        mask.transform.localScale = originalScale;
    }

    private static void ValidateRow(Transform[] row, Transform animationRoot = null)
    {
        Transform root = animationRoot != null ? animationRoot : row[0];
        Check(row.Length > 0 && row.All(slot => slot != null && slot.GetComponent<Image>() != null),
            "Every digit slot must have an Image");
        Check(root.GetComponent<Animator>() != null, "Keep the row entrance animation");
        Check(row.Where(slot => slot != root).All(slot => slot.parent == root && slot.GetComponent<Animator>() == null),
            "Child digits must inherit the row animation without overriding their positions");
    }

    private static void CheckScoreBounds(ResultSceneContext context)
    {
        RectTransform area = (RectTransform)RowRoot(context, context.ScoreParent);
        Image[] visible = area.GetComponentsInChildren<Image>()
            .Where(image => image.enabled).OrderBy(image => image.transform.position.x).ToArray();
        var corners = new Vector3[4];
        float previousRight = area.rect.xMin;
        float height = visible[0].rectTransform.rect.height;
        foreach (Image digit in visible)
        {
            digit.rectTransform.GetWorldCorners(corners);
            Vector3 min = area.InverseTransformPoint(corners[0]);
            Vector3 max = area.InverseTransformPoint(corners[2]);
            Check(min.x >= area.rect.xMin - 0.01f && max.x <= area.rect.xMax + 0.01f &&
                  min.y >= area.rect.yMin - 0.01f && max.y <= area.rect.yMax + 0.01f,
                "Every score digit must fit inside the score area");
            Check(min.x >= previousRight - 0.01f, "Score digits must not overlap");
            Check(Mathf.Abs(digit.rectTransform.rect.height - height) < 0.01f,
                "Score digits must have the same height");
            previousRight = max.x;
        }
        Check(Mathf.Abs(previousRight - area.rect.xMax) < 0.01f, "Right-align the final score");

        area.GetWorldCorners(corners);
        Transform canvas = area.GetComponentInParent<Canvas>().transform;
        Vector3 areaMin = canvas.InverseTransformPoint(corners[0]);
        Vector3 areaMax = canvas.InverseTransformPoint(corners[2]);
        Check(areaMin.x >= -235f && areaMax.x <= -15f && areaMin.y >= -180f && areaMax.y <= -120f,
            "Keep the score clear of the label, Pt and panel edges");
    }

    private static Transform RowRoot(ResultSceneContext context, Transform[] row)
    {
        return ReferenceEquals(row, context.ScoreParent) ? row[0].parent : row[0];
    }

    [MenuItem("Tools/Validation/Preview Result Score/1 digit (0)")]
    public static void PreviewOneDigit() => PreviewScore(0);

    [MenuItem("Tools/Validation/Preview Result Score/4 digits (3900)")]
    public static void PreviewFourDigits() => PreviewScore(3900);

    [MenuItem("Tools/Validation/Preview Result Score/7 digits (1234567)")]
    public static void PreviewSevenDigits() => PreviewScore(1234567);

    private static void PreviewScore(int value)
    {
        if (!EditorApplication.isPlaying) throw new InvalidOperationException("Run the preview in Play Mode.");
        ResultSceneContext context = UnityEngine.Object.FindFirstObjectByType<ResultSceneContext>();
        if (context == null) throw new InvalidOperationException("Open the Result scene before previewing.");
        // 再生中の表示だけを切り替え、実際の集計値やシーンファイルは変更しない。
        Show(context, context.ClearedWaveCount, context.ParrySuccessCount, value);
    }

    private static void Show(ResultSceneContext context, int waves, int parries, int score)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        typeof(ResultSceneContext).GetField("clearedWaveCount", flags).SetValue(context, waves);
        typeof(ResultSceneContext).GetField("parrySuccessCount", flags).SetValue(context, parries);
        typeof(ResultSceneContext).GetField("score", flags).SetValue(context, score);
        typeof(ResultSceneContext).GetMethod("RefreshDisplay", flags).Invoke(context, null);
    }

    private static void CheckRow(ResultSceneContext context, Transform[] row, string expected)
    {
        Image[] visible = RowRoot(context, row).GetComponentsInChildren<Image>(true)
            .Where(image => image.enabled && image.gameObject.activeInHierarchy)
            .OrderBy(image => image.transform.position.x).ToArray();
        string actual = string.Concat(visible.Select(image => Array.IndexOf(context.numberSprites, image.sprite)));
        Check(actual == expected, $"Expected {expected}, got {actual}");
        for (int i = 1; i < visible.Length; i++)
        {
            Check(visible[i].transform.position.x > visible[i - 1].transform.position.x,
                "Digits must have distinct horizontal positions");
        }
    }

    private static int CountImages(ResultSceneContext context)
    {
        return new[] { context.WaveParent, context.ParryParent, context.ScoreParent }
            .Sum(row => RowRoot(context, row).GetComponentsInChildren<Image>(true).Length);
    }

    private static void Check(bool success, string message)
    {
        if (!success) throw new InvalidOperationException(message);
        checks++;
    }
}
