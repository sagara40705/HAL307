using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance;

    [Header("フェード背景")]
    [SerializeField] private Image backgroundPanel;

    [Header("ダンベルUI設定")]
    [SerializeField] private RectTransform dumbbellCanvasRoot;
    [SerializeField] private GameObject dumbbellPrefab;

    [Header("演出開始時の落下設定")]
    [SerializeField, Min(0.1f)] private float revealDumbbellTotalDuration = 1f;
    [SerializeField, Range(0f, 0.9f)] private float revealFallStaggerRatio = 0.35f;
    [SerializeField, Range(0f, 1f)] private float presentationStartFadeRatio = 0.5f;

    [Header("シーンと同時に解放するUI")]
    [SerializeField] private GameObject[] sceneUiRoots;
    private Scene sceneUiOwner;

    [Header("遷移先シーン")]
    [SerializeField] private string nextSceneName = "GameScene";

    [Header("画面を埋める設定")]
    [SerializeField] private float cellWidth = 130f;
    [SerializeField] private float cellHeight = 80f;
    [SerializeField] private int extraRows = 3;

    [Header("落下演出設定")]
    [SerializeField] private float spawnInterval = 0.006f;
    [SerializeField] private float startYOffset = 250f;
    [SerializeField] private float fallSpeedMin = 1600f;
    [SerializeField] private float fallSpeedMax = 2400f;
    [SerializeField] private float rotateSpeedMin = 240f;
    [SerializeField] private float rotateSpeedMax = 720f;

    [Header("見た目のランダム")]
    [SerializeField] private float randomXOffset = 25f;
    [SerializeField] private float randomYOffset = 15f;
    [SerializeField] private float scaleMin = 1.15f;
    [SerializeField] private float scaleMax = 1.45f;

    private ObjectPool<GameObject> dumbbellPool;
    private readonly List<GameObject> activeDumbbells = new List<GameObject>();

    private bool isFading = false;
    private bool presentationRevealReady;
    private int transitionVersion;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            sceneUiOwner = gameObject.scene;
            DontDestroyOnLoad(gameObject);
            InitPool();
            SceneManager.sceneLoaded += OnSceneLoaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;

            if (backgroundPanel != null)
            {
                backgroundPanel.enabled = true;
                backgroundPanel.color = new Color(0f, 0f, 0f, 0f);
                backgroundPanel.gameObject.SetActive(false);
            }
        }
        else
        {
            // 常駐Canvasは再利用し、新しいUIシーンのGamePanelだけを引き継ぐ
            Instance.AdoptSceneUi(this);
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        Instance = null;
    }

    private void AdoptSceneUi(TransitionManager source)
    {
        if (source.sceneUiRoots == null || source.sceneUiRoots.Length == 0)
        {
            return;
        }

        ClearSceneUi();
        sceneUiOwner = source.gameObject.scene;
        sceneUiRoots = source.sceneUiRoots;
        source.sceneUiRoots = null;
        foreach (GameObject root in sceneUiRoots)
        {
            if (root != null)
            {
                root.transform.SetParent(transform, false);
                root.transform.SetAsFirstSibling();
            }
        }
        if (backgroundPanel != null)
        {
            backgroundPanel.transform.SetAsFirstSibling();
        }
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (scene.handle == sceneUiOwner.handle)
        {
            ClearSceneUi();
        }
    }

    // DontDestroyOnLoadへ移されたUIも、所有元シーンの解放時に破棄
    private void ClearSceneUi()
    {
        if (sceneUiRoots != null)
        {
            foreach (GameObject root in sceneUiRoots)
            {
                if (root != null)
                {
                    root.SetActive(false);
                    Destroy(root);
                }
            }
        }
        sceneUiRoots = null;
    }

    private void InitPool()
    {
        if (dumbbellPrefab == null) return;

        dumbbellPool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                GameObject obj = Instantiate(dumbbellPrefab);
                obj.SetActive(false);
                return obj;
            },
            actionOnGet: obj => obj.SetActive(true),
            actionOnRelease: obj => obj.SetActive(false),
            actionOnDestroy: obj => Destroy(obj),
            defaultCapacity: 300
        );
    }

    public static async UniTask PlayDumbbellFadeOut()
    {
        if (Instance == null)
        {
            Debug.LogWarning("TransitionManager.Instance が見つかりません");
            return;
        }

        await Instance.PlayDumbbellFadeOutInternal();
    }

    public static async UniTask FadeIn(float duration = 0.5f)
    {
        if (Instance == null)
        {
            Debug.LogWarning("TransitionManager.Instance が見つかりません");
            return;
        }

        await Instance.WaitForCurrentTransition();
        await Instance.FadeInInternal(duration);
    }

    // 通常モードは消去完了を待つ。既存の呼び出しは指定割合での再開を維持する。
    public static IEnumerator RevealIfCovered(float duration = 0.5f, bool waitForCompletion = false)
    {
        if (Instance == null || !Instance.HasVisibleTransition())
        {
            yield break;
        }

        if (waitForCompletion)
        {
            TransitionManager transition = Instance;
            // シーン切り替えで既に消去が始まっている場合は、その完了だけを待つ。
            yield return WaitForTransitionCompletion();
            // 落下中の全ダンベルと背景の消去まで含む、実際の処理完了を待つ。
            if (transition != null && transition == Instance && transition.HasVisibleTransition())
            {
                yield return transition.FadeInInternal(duration).ToCoroutine();
            }
            yield break;
        }

        Instance.presentationRevealReady = false;
        FadeIn(duration).Forget();
        while (Instance != null && Instance.isFading && !Instance.presentationRevealReady)
        {
            yield return null;
        }
    }

    public static void HoldBlackCover()
    {
        if (Instance == null || Instance.backgroundPanel == null)
        {
            return;
        }

        Image panel = Instance.backgroundPanel;
        panel.enabled = true;
        panel.color = new Color(0f, 0f, 0f, 1f);
        panel.gameObject.SetActive(true);
        panel.transform.SetAsFirstSibling();
    }

    public static void ResetImmediate()
    {
        if (Instance == null)
        {
            return;
        }

        Instance.ResetImmediateInternal();
    }

    // 進行中のダンベル遷移が実際に完了するまで待機する
    public static IEnumerator WaitForTransitionCompletion()
    {
        while (Instance != null && Instance.isFading)
        {
            yield return null;
        }
    }

    private async UniTask PlayDumbbellFadeOutInternal()
    {
        if (isFading) return;

        if (backgroundPanel == null || dumbbellCanvasRoot == null || dumbbellPrefab == null)
        {
            Debug.LogWarning("TransitionManager の参照設定が不足しています");
            return;
        }

        isFading = true;
        presentationRevealReady = false;
        int operationVersion = ++transitionVersion;

        CancellationToken token = this.GetCancellationTokenOnDestroy();

        ClearDumbbellsImmediate();

        backgroundPanel.gameObject.SetActive(true);
        backgroundPanel.enabled = true;
        backgroundPanel.transform.SetAsFirstSibling();

        Color baseColor = Color.black;
        backgroundPanel.color = new Color(0f, 0f, 0f, 0f);

        // 画面サイズ取得
        Rect rect = dumbbellCanvasRoot.rect;
        float screenWidth = rect.width;
        float screenHeight = rect.height;

        // 画面を埋めるための列・行数
        int columns = Mathf.CeilToInt(screenWidth / cellWidth) + 2;
        int rows = Mathf.CeilToInt(screenHeight / cellHeight) + extraRows;

        float startX = -screenWidth / 2f - cellWidth;
        float bottomY = -screenHeight / 2f - cellHeight * 0.5f;

        int totalCount = columns * rows;
        int spawnedCount = 0;

        // 下の段から順番に作ると、積もっていく感じが出る
        for (int row = 0; row < rows; row++)
        {
            // 偶数段と奇数段で半マスずらす
            float rowOffsetX = row % 2 == 0 ? 0f : cellWidth * 0.5f;

            for (int col = 0; col < columns; col++)
            {
                if (operationVersion != transitionVersion)
                {
                    return;
                }

                GameObject d = dumbbellPool.Get();
                d.transform.SetParent(dumbbellCanvasRoot, false);
                d.transform.SetAsLastSibling();

                RectTransform dRt = d.GetComponent<RectTransform>();

                float targetX =
                    startX +
                    col * cellWidth +
                    rowOffsetX +
                    Random.Range(-randomXOffset, randomXOffset);

                float targetY =
                    bottomY +
                    row * cellHeight +
                    Random.Range(-randomYOffset, randomYOffset);

                float startY =
                    screenHeight / 2f +
                    startYOffset +
                    row * 20f +
                    Random.Range(0f, 180f);

                Vector2 startPosition = new Vector2(targetX, startY);
                Vector2 targetPosition = new Vector2(targetX, targetY);

                float scale = Random.Range(scaleMin, scaleMax);
                dRt.localScale = Vector3.one * scale;

                dRt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

                UIDumbbellFall fall = d.GetComponent<UIDumbbellFall>();

                if (fall != null)
                {
                    fall.StartFall(
                        startPosition,
                        targetPosition,
                        Random.Range(fallSpeedMin, fallSpeedMax),
                        Random.Range(rotateSpeedMin, rotateSpeedMax)
                    );
                }
                else
                {
                    dRt.anchoredPosition = targetPosition;
                }

                activeDumbbells.Add(d);

                spawnedCount++;

                float alpha = Mathf.Clamp01((float)spawnedCount / totalCount);
                backgroundPanel.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha * 0.85f);

                await UniTask.Delay((int)(spawnInterval * 1000), cancellationToken: token);
            }
        }

        // ダンベルが落ち切るまで少し待つ
        if (!await WaitForDumbbellsToStop(operationVersion, token))
        {
            return;
        }

        backgroundPanel.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);

        // シーン切り替え
        //SceneManager.LoadScene(nextSceneName);

        if (operationVersion == transitionVersion)
        {
            isFading = false;
        }
    }

    private async UniTask FadeInInternal(float duration)
    {
        if (isFading) return;
        if (backgroundPanel == null) return;

        isFading = true;
        int operationVersion = ++transitionVersion;

        backgroundPanel.gameObject.SetActive(true);
        backgroundPanel.enabled = true;
        backgroundPanel.transform.SetAsFirstSibling();

        Color color = Color.black;
        backgroundPanel.color = new Color(0f, 0f, 0f, 1f);

        CancellationToken token = this.GetCancellationTokenOnDestroy();

        // ダンベルが存在する場合は、画面下へ落として消す
        if (activeDumbbells.Count > 0 && dumbbellCanvasRoot != null)
        {
            Rect rect = dumbbellCanvasRoot.rect;
            float screenHeight = rect.height;
            float fadeDuration = Mathf.Max(0.01f, duration);
            float fadeStartTime = Time.time;
            float totalFallDuration = Mathf.Max(0.1f, revealDumbbellTotalDuration);
            float staggerDuration = totalFallDuration * revealFallStaggerRatio;
            float individualFallDuration = Mathf.Max(0.1f, totalFallDuration - staggerDuration);

            float outTargetY = -screenHeight / 2f - 300f;

            // 下の方にあるダンベルから順番に落とすと自然
            activeDumbbells.Sort((a, b) =>
            {
                if (a == null || b == null) return 0;

                RectTransform ar = a.GetComponent<RectTransform>();
                RectTransform br = b.GetComponent<RectTransform>();

                return ar.anchoredPosition.y.CompareTo(br.anchoredPosition.y);
            });

            for (int i = 0; i < activeDumbbells.Count; i++)
            {
                if (operationVersion != transitionVersion)
                {
                    return;
                }

                float staggerRate = activeDumbbells.Count > 1
                    ? (float)i / (activeDumbbells.Count - 1)
                    : 0f;
                float scheduledStartTime = fadeStartTime + staggerDuration * staggerRate;

                while (Time.time < scheduledStartTime)
                {
                    if (operationVersion != transitionVersion)
                    {
                        return;
                    }

                    UpdateRevealFade(color, fadeDuration, Time.time - fadeStartTime);
                    await UniTask.Yield(token);
                }

                GameObject d = activeDumbbells[i];
                if (d == null) continue;

                UIDumbbellFall fall = d.GetComponent<UIDumbbellFall>();

                if (fall != null)
                {
                    RectTransform dumbbellRect = d.GetComponent<RectTransform>();
                    float fallDistance = dumbbellRect != null
                        ? Mathf.Max(1f, dumbbellRect.anchoredPosition.y - outTargetY)
                        : screenHeight + 300f;
                    float initialSpeed = fallDistance * 0.5f / individualFallDuration;
                    float acceleration = fallDistance /
                                         (individualFallDuration * individualFallDuration);

                    fall.StartFallOut(
                        outTargetY,
                        initialSpeed,
                        acceleration,
                        Random.Range(-120f, 120f),
                        Random.Range(360f, 900f)
                    );
                }
            }

            float concurrentFadeTime = Time.time - fadeStartTime;

            while (concurrentFadeTime < fadeDuration)
            {
                if (operationVersion != transitionVersion)
                {
                    return;
                }

                concurrentFadeTime += Time.deltaTime;

                UpdateRevealFade(color, fadeDuration, concurrentFadeTime);

                await UniTask.Yield(token);
            }

            // 全ダンベルが下に落ちるまで少し待つ
            if (!await WaitForDumbbellsToStop(operationVersion, token))
            {
                return;
            }

            ClearDumbbellsImmediate();

            backgroundPanel.color = new Color(color.r, color.g, color.b, 0f);
            backgroundPanel.gameObject.SetActive(false);
            presentationRevealReady = true;

            if (operationVersion == transitionVersion)
            {
                isFading = false;
            }
            return;
        }

        // ダンベルがない場合は普通のフェード
        float fadeTime = 0f;

        while (fadeTime < duration)
        {
            if (operationVersion != transitionVersion)
            {
                return;
            }

            fadeTime += Time.deltaTime;

            float alpha = Mathf.Lerp(1f, 0f, fadeTime / duration);
            backgroundPanel.color = new Color(color.r, color.g, color.b, alpha);

            if (fadeTime / Mathf.Max(0.01f, duration) >= presentationStartFadeRatio)
            {
                presentationRevealReady = true;
            }

            await UniTask.Yield(token);
        }

        backgroundPanel.color = new Color(color.r, color.g, color.b, 0f);
        backgroundPanel.gameObject.SetActive(false);
        presentationRevealReady = true;

        if (operationVersion == transitionVersion)
        {
            isFading = false;
        }
    }

    private void UpdateRevealFade(Color color, float duration, float elapsed)
    {
        float progress = Mathf.Clamp01(elapsed / duration);
        backgroundPanel.color = new Color(color.r, color.g, color.b, 1f - progress);

        if (progress >= presentationStartFadeRatio)
        {
            presentationRevealReady = true;
        }
    }

    private async UniTask WaitForCurrentTransition()
    {
        CancellationToken token = this.GetCancellationTokenOnDestroy();
        while (isFading)
        {
            await UniTask.Yield(token);
        }
    }

    private async UniTask<bool> WaitForDumbbellsToStop(int operationVersion, CancellationToken token)
    {
        while (operationVersion == transitionVersion && HasMovingDumbbell())
        {
            await UniTask.Yield(token);
        }

        return operationVersion == transitionVersion;
    }

    private bool HasMovingDumbbell()
    {
        for (int i = 0; i < activeDumbbells.Count; i++)
        {
            GameObject dumbbell = activeDumbbells[i];
            if (dumbbell == null)
            {
                continue;
            }

            UIDumbbellFall fall = dumbbell.GetComponent<UIDumbbellFall>();
            if (fall != null && fall.IsMoving)
            {
                return true;
            }
        }

        return false;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single || !HasVisibleTransition())
        {
            return;
        }

        FadeInAfterSceneLoad().Forget();
    }

    private async UniTaskVoid FadeInAfterSceneLoad()
    {
        while (this != null && isFading)
        {
            await UniTask.Yield();
        }

        if (this != null && HasVisibleTransition())
        {
            await FadeInInternal(0.5f);
        }
    }

    private bool HasVisibleTransition()
    {
        return activeDumbbells.Count > 0 ||
               (backgroundPanel != null && backgroundPanel.gameObject.activeSelf);
    }

    private void ClearDumbbellsImmediate()
    {
        if (dumbbellPool == null) return;

        for (int i = 0; i < activeDumbbells.Count; i++)
        {
            if (activeDumbbells[i] != null)
            {
                dumbbellPool.Release(activeDumbbells[i]);
            }
        }

        activeDumbbells.Clear();
    }

    private void ResetImmediateInternal()
    {
        transitionVersion++;
        isFading = false;
        ClearDumbbellsImmediate();

        if (backgroundPanel != null)
        {
            Color color = backgroundPanel.color;
            backgroundPanel.color = new Color(color.r, color.g, color.b, 0f);
            backgroundPanel.gameObject.SetActive(false);
        }
    }
}
