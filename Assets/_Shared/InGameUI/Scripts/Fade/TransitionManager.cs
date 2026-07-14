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
    private int transitionVersion;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitPool();
            SceneManager.sceneLoaded += OnSceneLoaded;

            if (backgroundPanel != null)
            {
                Color color = backgroundPanel.color;
                backgroundPanel.color = new Color(color.r, color.g, color.b, 0f);
                backgroundPanel.gameObject.SetActive(false);
            }
        }
        else
        {
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
        Instance = null;
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
        int operationVersion = ++transitionVersion;

        CancellationToken token = this.GetCancellationTokenOnDestroy();

        ClearDumbbellsImmediate();

        backgroundPanel.gameObject.SetActive(true);
        backgroundPanel.transform.SetAsFirstSibling();

        Color baseColor = backgroundPanel.color;
        backgroundPanel.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);

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
        backgroundPanel.transform.SetAsFirstSibling();

        Color color = backgroundPanel.color;
        backgroundPanel.color = new Color(color.r, color.g, color.b, 1f);

        CancellationToken token = this.GetCancellationTokenOnDestroy();

        // ダンベルが存在する場合は、画面下へ落として消す
        if (activeDumbbells.Count > 0 && dumbbellCanvasRoot != null)
        {
            Rect rect = dumbbellCanvasRoot.rect;
            float screenHeight = rect.height;

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

                GameObject d = activeDumbbells[i];
                if (d == null) continue;

                UIDumbbellFall fall = d.GetComponent<UIDumbbellFall>();

                if (fall != null)
                {
                    fall.StartFallOut(
                        outTargetY,
                        Random.Range(300f, 700f),
                        Random.Range(1800f, 2800f),
                        Random.Range(-120f, 120f),
                        Random.Range(360f, 900f)
                    );
                }

                // 少しずつ崩れる感じ
                await UniTask.Delay(8, cancellationToken: token);
            }

            // 背景も同時に薄くする
            float time = 0f;

            while (time < duration)
            {
                if (operationVersion != transitionVersion)
                {
                    return;
                }

                time += Time.deltaTime;

                float alpha = Mathf.Lerp(1f, 0f, time / duration);
                backgroundPanel.color = new Color(color.r, color.g, color.b, alpha);

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

            await UniTask.Yield(token);
        }

        backgroundPanel.color = new Color(color.r, color.g, color.b, 0f);
        backgroundPanel.gameObject.SetActive(false);

        if (operationVersion == transitionVersion)
        {
            isFading = false;
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
