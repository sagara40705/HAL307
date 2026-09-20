using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MonitorBreakController : MonoBehaviour
{

    // このオブジェクトに付いているRenderer
    private Renderer targetRenderer;

    // 実行中に操作する専用Material
    private Material runtimeMaterial;

    // Shaderの「_BreakAmount」という名前をIDへ変換する。
    private static readonly int BreakAmountId =
        Shader.PropertyToID("_BreakAmount");

    // Enterを押したときに生成する破壊エフェクト
    [SerializeField]
    private GameObject breakEffectPrefab;

    // エフェクトを表示する位置
    [SerializeField]
    private Transform effectSpawnPoint;

    [SerializeField, Min(0.01f)]
    private float breakEffectScale = 1.0f;

    private void Awake()
    {
        // このスクリプトを付けたオブジェクトから
        // MeshRendererなどのRendererを探す。
        targetRenderer = GetComponent<Renderer>();

        // Rendererが見つからなければ、設定できないのでエラーを出す。
        if (targetRenderer == null)
        {
            Debug.LogError(
                "MonitorBreakControllerと同じオブジェクトにRendererがありません。",
                this
            );

            return;
        }

        // materialを取得すると、このオブジェクト専用のMaterialが作られる。
        // ほかのオブジェクトで同じMaterialを使っていても影響しない。
        runtimeMaterial = targetRenderer.material;

        // ゲーム開始時は破壊表示をOFFにする。
        // 0 = 通常のモニター表示
        ResetMonitor();
    }

    private void Update()
    {
        // キーボードが接続されていない場合は何もしない。
        if (Keyboard.current == null)
        {
            return;
        }

        // Enterキーを押した瞬間だけ実行する。
        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            BreakMonitor();
        }
    }

    /// <summary>
    /// Shaderの破壊表示をONにする。
    /// </summary>
    public void ResetMonitor()
    {
        if (runtimeMaterial != null)
        {
            runtimeMaterial.SetFloat(BreakAmountId, 0.0f);
        }
    }

    public void BreakMonitor()
    {
        Vector3 spawnPosition = effectSpawnPoint != null
            ? effectSpawnPoint.position
            : transform.position;
        BreakMonitor(spawnPosition);
    }

    public void BreakMonitor(Vector3 spawnPosition)
    {
        if (runtimeMaterial == null)
        {
            return;
        }

        // 1 = 破壊表示ON
        runtimeMaterial.SetFloat(BreakAmountId, 1.0f);

        //エフェクトが無い場合shaderだけ変更して終了する
        if (breakEffectPrefab == null)
        {
            Debug.LogWarning(
                "Break Effect Prefabが設定されていません。",
                this
            );

            return;
        }

        // 破壊エフェクトを生成する。
        GameObject effrctInstanece = Instantiate(
            breakEffectPrefab,
            spawnPosition,
            Quaternion.identity
        );
        SceneManager.MoveGameObjectToScene(effrctInstanece, gameObject.scene);
        effrctInstanece.transform.localScale *= breakEffectScale;

        // このVFXはPrefab内のParticle Systemに大きなローカル座標が入っているため、
        // 主Particle Systemの位置が指定した破壊中心へ来るようルートを補正する。
        ParticleSystem effectAnchor = effrctInstanece.GetComponentInChildren<ParticleSystem>(true);
        if (effectAnchor != null)
        {
            effrctInstanece.transform.position += spawnPosition - effectAnchor.transform.position;
        }

        //5秒後破壊エフェクトを削除する。
        Destroy(effrctInstanece, 5.0f);

    }

    private void OnDestroy()
    {
        // Renderer.materialで実行中に作られたMaterialを破棄する。
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
    }
}
