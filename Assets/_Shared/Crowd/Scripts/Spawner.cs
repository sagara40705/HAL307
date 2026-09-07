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

        // One property block per chunk.
        public MaterialPropertyBlock propertyBlock;
    }

    [Header("Rendering")]
    [SerializeField] private Mesh mesh;
    [SerializeField] private Material material;

    [Header("Rendering Options")]
    [SerializeField] private bool castShadows = false;
    [SerializeField] private bool receiveShadows = false;

    // Set this to your Crowd layer if you create one.
    [SerializeField] private int renderLayer = 0;

    [Header("Scale")]
    [SerializeField] private Vector3 modelScale = Vector3.one;

    [Header("Crowd Size")]
    [SerializeField] private int rows = 20;
    [SerializeField] private int cols = 20;
    [SerializeField] private float spacing = 1.5f;
    [SerializeField] private float rowHeight = 0.3f;

    [Header("Chunk Settings")]
    // 20 x 20 crowd with 10 x 10 chunks = 4 chunks.
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
    // CAMERA FRUSTUM CACHE
    // =========================================================

    private class CameraFrustumData
    {
        public Camera camera;

        // Unity expects exactly 6 planes.
        public readonly Plane[] planes = new Plane[6];

        public int frame = -1;
    }

    private static readonly Dictionary<int, CameraFrustumData>
        cameraFrustumCache =
        new Dictionary<int, CameraFrustumData>();


    // =========================================================
    // START
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

        // VAT animation can move vertices outside the original mesh bounds.
        mesh.bounds = new Bounds(
            Vector3.zero,
            vatBoundsSize
        );

        GenerateCrowd();
    }


    // =========================================================
    // CROWD GENERATION
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
    // CREATE CHUNK
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
        // CREATE PEOPLE
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


                // Each character gets its own animation offset.
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
        // CREATE CHUNK
        // -----------------------------------------------------

        CrowdChunk chunk =
            new CrowdChunk();


        chunk.matrices =
            matrices.ToArray();


        chunk.timeOffsets =
            timeOffsets.ToArray();


        // -----------------------------------------------------
        // CHUNK BOUNDS
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
        // MATERIAL PROPERTY BLOCK
        // -----------------------------------------------------

        chunk.propertyBlock =
            new MaterialPropertyBlock();


        // Animation index is now ONE value.
        //
        // Before:
        // _AnimIndex = [0,0,0,0,0,0,...]
        //
        // Now:
        // _AnimIndex = 0
        //
        chunk.propertyBlock.SetFloat(
            AnimIndexID,
            animationIndex
        );


        // Time offset remains per-instance.
        chunk.propertyBlock.SetFloatArray(
            DTID,
            chunk.timeOffsets
        );


        return chunk;
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // -----------------------------------------------------
        // TEST ANIMATIONS
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
        // NO CAMERAS
        // -----------------------------------------------------

        if (renderCameras == null ||
            renderCameras.Length == 0)
        {
            return;
        }


        // -----------------------------------------------------
        // WHOLE CROWD CULL
        // -----------------------------------------------------

        if (!IsBoundsVisible(crowdBounds))
        {
            return;
        }


        // -----------------------------------------------------
        // CHUNK CULL + DRAW
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
    // FRUSTUM CULLING
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


            // Calculate planes only once per camera per frame.
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
    // CHANGE ANIMATION
    // =========================================================

    public void SetAnimationIndex(
        int index
    )
    {
        // Don't do anything if the animation is already active.
        if (animationIndex == index)
        {
            return;
        }


        animationIndex =
            index;


        ApplyAnimationIndex();
    }


    // =========================================================
    // APPLY ANIMATION
    // =========================================================

    private void ApplyAnimationIndex()
    {
        // IMPORTANT:
        //
        // We no longer loop through every character.
        //
        // _AnimIndex is a single material property.
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
    // DEBUG GIZMOS
    // =========================================================

    private void OnDrawGizmos()
    {
        if (!showChunkBounds)
            return;

        // Draw an approximate overall crowd area even before Play mode.
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


        // Draw actual generated chunk bounds during Play Mode.
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
