using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Pool;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance;

    [Header("フェード設定")]
    [SerializeField] private Image backgroundPanel;

    [Header("ダンベル落下設定")]
    [SerializeField] private Transform dumbbellCanvasRoot;
    [SerializeField] private GameObject dumbbellPrefab;
    [SerializeField] private int spawnCount = 80;
    [SerializeField] private float spawnInterval = 0.02f;
    [SerializeField] private float spawnSize = 1f;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private float spawnWidth = 400f;

    private ObjectPool<GameObject> dumbbellPool;
    private readonly List<GameObject> activeDumbbells = new List<GameObject>();

    private bool isFading = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitPool();

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
            defaultCapacity: spawnCount
        );
    }

    /// <summary>
    /// どこからでも呼べるダンベルフェードアウト
    /// ダンベルが降って画面が埋まった状態で終了する
    /// await TransitionManager.PlayDumbbellFadeOut();
    /// </summary>
    public static async UniTask PlayDumbbellFadeOut()
    {
        if (Instance == null)
        {
            Debug.LogWarning("TransitionManager.Instance が見つかりません");
            return;
        }

        await Instance.PlayDumbbellFadeOutInternal();
    }

    /// <summary>
    /// どこからでも呼べる通常フェードイン
    /// 画面を開きつつ、残っているダンベルを回収する
    /// await TransitionManager.FadeIn();
    /// </summary>
    public static async UniTask FadeIn(float duration = 0.5f)
    {
        if (Instance == null)
        {
            Debug.LogWarning("TransitionManager.Instance が見つかりません");
            return;
        }

        await Instance.FadeInInternal(duration);
    }

    private async UniTask PlayDumbbellFadeOutInternal()
    {
        if (isFading) return;
        if (backgroundPanel == null || dumbbellCanvasRoot == null || dumbbellPrefab == null || spawnPoint == null)
        {
            Debug.LogWarning("TransitionManager の参照設定が不足しています");
            return;
        }

        isFading = true;
        CancellationToken token = this.GetCancellationTokenOnDestroy();

        ClearDumbbellsImmediate();

        backgroundPanel.gameObject.SetActive(true);
        backgroundPanel.transform.SetAsFirstSibling();

        Color baseColor = backgroundPanel.color;
        backgroundPanel.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);

        RectTransform spawnRt = spawnPoint as RectTransform;

        for (int i = 0; i < spawnCount; i++)
        {
            GameObject d = dumbbellPool.Get();
            d.transform.SetParent(dumbbellCanvasRoot, false);
            d.transform.SetAsLastSibling();

            RectTransform dRt = d.GetComponent<RectTransform>();

            float randomX = Random.Range(-spawnWidth, spawnWidth);

            dRt.anchoredPosition = new Vector2(
                spawnRt.anchoredPosition.x + randomX,
                spawnRt.anchoredPosition.y
            );

            dRt.localScale = Vector3.one * spawnSize;
            dRt.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            var fall = d.GetComponent<UIDumbbellFall>();
            if (fall != null)
            {
                fall.ResetFall(-500f, 500f, -250f);
            }

            activeDumbbells.Add(d);

            float alpha = Mathf.Clamp01((float)(i + 1) / spawnCount);
            backgroundPanel.color = new Color(baseColor.r, baseColor.g, baseColor.b, alpha);

            await UniTask.Delay((int)(spawnInterval * 1000), cancellationToken: token);
        }

        backgroundPanel.color = new Color(baseColor.r, baseColor.g, baseColor.b, 1f);
        isFading = false;
    }


    private async UniTask FadeInInternal(float duration)
    {
        if (isFading) return;
        if (backgroundPanel == null) return;

        isFading = true;

        backgroundPanel.gameObject.SetActive(true);
        backgroundPanel.transform.SetAsFirstSibling();

        Color color = backgroundPanel.color;
        backgroundPanel.color = new Color(color.r, color.g, color.b, 1f);

        // 必要ならここで先にダンベルを消す
        ClearDumbbellsImmediate();

        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, time / duration);
            backgroundPanel.color = new Color(color.r, color.g, color.b, alpha);
            await UniTask.Yield();
        }

        backgroundPanel.color = new Color(color.r, color.g, color.b, 0f);
        backgroundPanel.gameObject.SetActive(false);

        isFading = false;
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
}
