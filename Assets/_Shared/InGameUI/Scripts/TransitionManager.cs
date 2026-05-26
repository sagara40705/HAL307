using UnityEngine;
using UnityEngine.Pool;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.Threading;

public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance;

    [Header("ダンベル落下設定")]
    [SerializeField] private Transform dumbbellCanvasRoot;
    public GameObject dumbbellPrefab;
    public int spawnCount = 80;
    public float spawnInterval = 0.02f;
    public int spawnSize;
    public UnityEngine.UI.Image backgroundPanel;

    [Tooltip("Canvas内の上の方に置いた空オブジェクトをセット")]
    public Transform spawnPoint;
    public float spawnWidth = 400f; // UIのピクセル幅に合わせて調整

    private ObjectPool<GameObject> dumbbellPool;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitPool();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitPool()
    {
        dumbbellPool = new ObjectPool<GameObject>(
            createFunc: () => {
                // Managerの子として生成
                GameObject obj = Instantiate(dumbbellPrefab, this.transform);
                obj.SetActive(false);
                return obj;
            },
            actionOnGet: (obj) => obj.SetActive(true),
            actionOnRelease: (obj) => obj.SetActive(false),
            actionOnDestroy: (obj) => Destroy(obj),
            defaultCapacity: spawnCount
        );
    }

    private void OnEnable()
    {
        // シーンが読み込まれたときに実行されるイベントを登録
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // 新しいシーンが読み込まれた瞬間に呼ばれる
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //// 今あるメインカメラを探す
        //Camera mainCam = Camera.main;

        //// シーン内のすべてのCanvasを探す
        //Canvas[] canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
       
        //foreach (var c in canvases)
        //{
        //    // ダンベル用Canvasはスキップ
        //    if (c.gameObject == this.gameObject) continue;
           
        //    // タイトル画面などのCanvasを強制的にカメラモードに書き換える
        //    c.renderMode = RenderMode.ScreenSpaceCamera;
        //    c.worldCamera = mainCam;

        //    c.planeDistance = 100; // シーン遷移のUIより遠くに配置
        //    c.sortingOrder = 0;   //
        //}
    }



    public async UniTask StartDumbbellTransition(string nextSceneName)
    {
        CancellationToken token = this.GetCancellationTokenOnDestroy();
        List<GameObject> spawnedDumbbells = new List<GameObject>();

        backgroundPanel.gameObject.SetActive(true);

        Color baseColor = backgroundPanel.color;
        backgroundPanel.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
        backgroundPanel.transform.SetAsFirstSibling();

        backgroundPanel.transform.SetAsFirstSibling(); // 黒背景は一番奥
        // ダンベル生成
        for (int i = 0; i < spawnCount; i++)
        {


            // 生成時
            GameObject d = dumbbellPool.Get();
            d.transform.SetParent(dumbbellCanvasRoot, false);
            d.transform.SetAsLastSibling();
            spawnedDumbbells.Add(d);

            float a = Mathf.Clamp01(backgroundPanel.color.a + 0.01f);
            backgroundPanel.color = new Color(
                baseColor.r, baseColor.g, baseColor.b, a);

            // 座標の設定
            Vector3 basePos = spawnPoint.localPosition;
            float randomX = Random.Range(-spawnWidth, spawnWidth);

          
            d.transform.localPosition = new Vector3(basePos.x + randomX, basePos.y, 0f);
            d.transform.localScale = Vector3.one * spawnSize;
            d.transform.localRotation = Quaternion.Euler(0, 0, Random.Range(0, 360));


            await UniTask.Delay((int)(spawnInterval * 1000), cancellationToken: token);
        }

        //画面が埋まるまで待機
        await UniTask.Delay(2000, cancellationToken: token);

        //シーン遷移
        if (SceneController.Instance != null)
        {
            await SceneController.Instance.ChangeScene(nextSceneName);
        }

        //遷移後の回収
        await UniTask.Delay(100, cancellationToken: token);
        foreach (var d in spawnedDumbbells)
        {
            dumbbellPool.Release(d);
        }
    }
}