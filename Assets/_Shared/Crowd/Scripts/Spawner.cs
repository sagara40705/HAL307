using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class CrowdSpawner : MonoBehaviour
{
    [System.Serializable]
    public class CrowdChunk
    {
        public Matrix4x4[] matrices;
        public float[] timeOffsets;
        public Bounds bounds;

        // チャンクごとにプロパティブロックを1つ用意する。
        public MaterialPropertyBlock propertyBlock;
    }

    [Header("Rendering")]
    [SerializeField] private Mesh mesh;
    [SerializeField] private Material material;

    [Header("Rendering Options")]
    [SerializeField] private bool castShadows = false;
    [SerializeField] private bool receiveShadows = false;

    // 観客用のレイヤーを作成した場合は、ここに設定する。
    [SerializeField] private int renderLayer = 0;

    [Header("Scale")]
    [SerializeField] private Vector3 modelScale = Vector3.one;

    [Header("Crowd Size")]
    [SerializeField] private int rows = 20;
    [SerializeField] private int cols = 20;
    [SerializeField] private float spacing = 1.5f;
    [SerializeField] private float rowHeight = 0.3f;

    [Header("Chunk Settings")]
    // 20×20人の観客を10×10人のチャンクに分けると、チャンクは4個になる。
    [SerializeField] private int chunkRows = 10;
    [SerializeField] private int chunkCols = 10;

    [Header("Animation")]
    [SerializeField] private int animationIndex = 0;

    [Header("VAT Bounds")]
    [SerializeField] private Vector3 vatBoundsSize = new Vector3(5f, 5f, 5f);

    [Header("Debug")]
    [SerializeField] private bool showChunkBounds = true;

    [Header("Culling")]
    [SerializeField] private Camera[] renderCameras;

    private readonly List<CrowdChunk> chunks = new List<CrowdChunk>();

    private Bounds crowdBounds;

    private static readonly int AnimIndexID =
        Shader.PropertyToID("_AnimIndex");

    private static readonly int DTID =
        Shader.PropertyToID("_DT");


    // =========================================================
    // カメラの視錐台キャッシュ
    // =========================================================

    private class CameraFrustumData
    {
        public Camera camera;

        // Unityでは平面が正確に6枚必要になる。
        public readonly Plane[] planes = new Plane[6];

        public int frame = -1;
    }

    private static readonly Dictionary<int, CameraFrustumData>
        cameraFrustumCache =
        new Dictionary<int, CameraFrustumData>();


    // =========================================================
    // 開始処理
    // =========================================================

    private void Start()
    {
        if (mesh == null)
        {
            Debug.LogError(
                "CrowdSpawner: Mesh is missing.",
                this
            );

            enabled = false;
            return;
        }

        if (material == null)
        {
            Debug.LogError(
                "CrowdSpawner: Material is missing.",
                this
            );

            enabled = false;
            return;
        }

        if (!material.enableInstancing)
        {
            Debug.LogWarning(
                "CrowdSpawner: Enable GPU Instancing on the material.",
                this
            );
        }

        // VATアニメーションでは、頂点が元のメッシュの境界外へ移動する場合がある。
        mesh.bounds = new Bounds(
            Vector3.zero,
            vatBoundsSize
        );

        GenerateCrowd();
    }


    // =========================================================
    // 観客の生成
    // =========================================================

    private void GenerateCrowd()
    {
        chunks.Clear();

        chunkRows = Mathf.Max(1, chunkRows);
        chunkCols = Mathf.Max(1, chunkCols);

        int chunkGridRows =
            Mathf.CeilToInt((float)rows / chunkRows);

        int chunkGridCols =
            Mathf.CeilToInt((float)cols / chunkCols);


        Vector3 overallMin =
            new Vector3(
                float.MaxValue,
                float.MaxValue,
                float.MaxValue
            );

        Vector3 overallMax =
            new Vector3(
                float.MinValue,
                float.MinValue,
                float.MinValue
            );


        for (int chunkR = 0; chunkR < chunkGridRows; chunkR++)
        {
            for (int chunkC = 0; chunkC < chunkGridCols; chunkC++)
            {
                CrowdChunk chunk =
                    CreateChunk(chunkR, chunkC);

                chunks.Add(chunk);

                overallMin =
                    Vector3.Min(
                        overallMin,
                        chunk.bounds.min
                    );

                overallMax =
                    Vector3.Max(
                        overallMax,
                        chunk.bounds.max
                    );
            }
        }


        crowdBounds = new Bounds();

        crowdBounds.SetMinMax(
            overallMin,
            overallMax
        );
    }


    // =========================================================
    // チャンクの作成
    // =========================================================

    private CrowdChunk CreateChunk(
        int chunkRow,
        int chunkCol
    )
    {
        List<Matrix4x4> matrices =
            new List<Matrix4x4>();

        List<float> timeOffsets =
            new List<float>();


        int startRow =
            chunkRow * chunkRows;

        int endRow =
            Mathf.Min(
                startRow + chunkRows,
                rows
            );


        int startCol =
            chunkCol * chunkCols;

        int endCol =
            Mathf.Min(
                startCol + chunkCols,
                cols
            );


        float centerOffset =
            (cols - 1) * 0.5f;


        Vector3 min =
            new Vector3(
                float.MaxValue,
                float.MaxValue,
                float.MaxValue
            );

        Vector3 max =
            new Vector3(
                float.MinValue,
                float.MinValue,
                float.MinValue
            );


        // -----------------------------------------------------
        // 人物の作成
        // -----------------------------------------------------

        for (int r = startRow; r < endRow; r++)
        {
            for (int c = startCol; c < endCol; c++)
            {
                Vector3 columnOffset =
                    Vector3.right *
                    ((c - centerOffset) * spacing);


                Vector3 rowOffset =
                    Vector3.forward *
                    (r * spacing);


                Vector3 heightOffset =
                    Vector3.up *
                    ((rows - 1 - r) * rowHeight);


                Vector3 localPosition =
                    columnOffset +
                    rowOffset +
                    heightOffset;


                Vector3 worldPosition =
                    transform.position +
                    transform.rotation *
                    localPosition;


                Quaternion randomRotation =
                    Quaternion.Euler(
                        0f,
                        Random.Range(-15f, 15f),
                        0f
                    );


                Quaternion finalRotation =
                    transform.rotation *
                    randomRotation;


                Matrix4x4 matrix =
                    Matrix4x4.TRS(
                        worldPosition,
                        finalRotation,
                        modelScale
                    );


                matrices.Add(matrix);


                // 各キャラクターに個別のアニメーションオフセットを設定する。
                timeOffsets.Add(
                    Random.Range(0f, 1f)
                );


                min =
                    Vector3.Min(
                        min,
                        worldPosition
                    );

                max =
                    Vector3.Max(
                        max,
                        worldPosition
                    );
            }
        }


        // -----------------------------------------------------
        // チャンクの作成
        // -----------------------------------------------------

        CrowdChunk chunk =
            new CrowdChunk();


        chunk.matrices =
            matrices.ToArray();


        chunk.timeOffsets =
            timeOffsets.ToArray();


        // -----------------------------------------------------
        // チャンクの境界
        // -----------------------------------------------------

        Vector3 padding =
            new Vector3(
                2f,
                3f,
                2f
            );


        chunk.bounds =
            new Bounds();


        chunk.bounds.SetMinMax(
            min - padding,
            max + padding
        );


        // -----------------------------------------------------
        // マテリアルプロパティブロック
        // -----------------------------------------------------

        chunk.propertyBlock =
            new MaterialPropertyBlock();


        // アニメーション番号は現在、単一の値として保持する。
        //
        // 変更前：
        // _AnimIndex = [0,0,0,0,0,0,...]
        //
        // 変更後：
        // _AnimIndex = 0
        //
        chunk.propertyBlock.SetFloat(
            AnimIndexID,
            animationIndex
        );


        // 時間オフセットは引き続きインスタンスごとに保持する。
        chunk.propertyBlock.SetFloatArray(
            DTID,
            chunk.timeOffsets
        );


        return chunk;
    }


    // =========================================================
    // 更新処理
    // =========================================================

    private void Update()
    {
        // -----------------------------------------------------
        // アニメーションのテスト
        // -----------------------------------------------------

        if (Keyboard.current != null)
        {
            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                SetAnimationIndex(0);
            }

            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                SetAnimationIndex(1);
            }
        }


        // -----------------------------------------------------
        // カメラが存在しない場合
        // -----------------------------------------------------

        if (renderCameras == null ||
            renderCameras.Length == 0)
        {
            return;
        }


        // -----------------------------------------------------
        // 観客全体のカリング
        // -----------------------------------------------------

        if (!IsBoundsVisible(crowdBounds))
        {
            return;
        }


        // -----------------------------------------------------
        // チャンクのカリングと描画
        // -----------------------------------------------------

        for (int i = 0; i < chunks.Count; i++)
        {
            CrowdChunk chunk =
                chunks[i];


            if (!IsBoundsVisible(chunk.bounds))
            {
                continue;
            }


            Graphics.DrawMeshInstanced(
                mesh,
                0,
                material,
                chunk.matrices,
                chunk.matrices.Length,
                chunk.propertyBlock,

                castShadows
                    ? ShadowCastingMode.On
                    : ShadowCastingMode.Off,

                receiveShadows,

                renderLayer
            );
        }
    }


    // =========================================================
    // 視錐台カリング
    // =========================================================

    private bool IsBoundsVisible(
        Bounds bounds
    )
    {
        int frame =
            Time.frameCount;


        for (int i = 0;
             i < renderCameras.Length;
             i++)
        {
            Camera cam =
                renderCameras[i];


            if (cam == null)
            {
                continue;
            }


            int cameraID =
                cam.GetInstanceID();


            CameraFrustumData data;


            if (!cameraFrustumCache.TryGetValue(
                cameraID,
                out data))
            {
                data =
                    new CameraFrustumData();

                data.camera =
                    cam;


                cameraFrustumCache.Add(
                    cameraID,
                    data
                );
            }


            // 平面の計算は、各カメラにつき1フレームに1回だけ行う。
            if (data.frame != frame ||
                data.camera != cam)
            {
                data.camera =
                    cam;


                GeometryUtility.CalculateFrustumPlanes(
                    cam,
                    data.planes
                );


                data.frame =
                    frame;
            }


            if (GeometryUtility.TestPlanesAABB(
                data.planes,
                bounds))
            {
                return true;
            }
        }


        return false;
    }


    // =========================================================
    // アニメーションの変更
    // =========================================================

    public void SetAnimationIndex(
        int index
    )
    {
        // 同じアニメーションが既に有効なら何もしない。
        if (animationIndex == index)
        {
            return;
        }


        animationIndex =
            index;


        ApplyAnimationIndex();
    }


    // =========================================================
    // アニメーションの適用
    // =========================================================

    private void ApplyAnimationIndex()
    {
        // 重要：
        //
        // 全キャラクターを巡回する処理は行わない。
        //
        // _AnimIndexは単一のマテリアルプロパティとして扱う。
        //
        for (int c = 0;
             c < chunks.Count;
             c++)
        {
            chunks[c].propertyBlock.SetFloat(
                AnimIndexID,
                animationIndex
            );
        }
    }


    // =========================================================
    // デバッグ用ギズモ
    // =========================================================

    private void OnDrawGizmos()
    {
        if (!showChunkBounds)
            return;

        // 再生モードに入る前も、観客全体のおおよその範囲を描画する。
        Vector3 centerOffset = new Vector3(
            0f,
            ((rows - 1) * rowHeight) * 0.5f,
            ((rows - 1) * spacing) * 0.5f
        );

        float width = cols * spacing;
        float depth = rows * spacing;
        float height = rows * rowHeight + 3f;

        Vector3 localCenter = centerOffset;

        Vector3 worldCenter =
            transform.position +
            transform.rotation * localCenter;

        Vector3 size = new Vector3(
            width,
            height,
            depth
        );

        Gizmos.color = Color.yellow;

        Matrix4x4 oldMatrix = Gizmos.matrix;

        Gizmos.matrix = Matrix4x4.TRS(
            worldCenter,
            transform.rotation,
            Vector3.one
        );

        Gizmos.DrawWireCube(
            Vector3.zero,
            size
        );

        Gizmos.matrix = oldMatrix;


        // 再生中は、実際に生成されたチャンクの境界を描画する。
        if (chunks == null || chunks.Count == 0)
            return;

        Gizmos.color = Color.green;

        for (int i = 0; i < chunks.Count; i++)
        {
            Gizmos.DrawWireCube(
                chunks[i].bounds.center,
                chunks[i].bounds.size
            );
        }
    }
}
