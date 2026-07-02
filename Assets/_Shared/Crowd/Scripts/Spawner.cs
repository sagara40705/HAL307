using System.Collections.Generic;
using Unity.AppUI.Redux;
using UnityEngine;
using UnityEngine.InputSystem;

public class CrowdSpawner : MonoBehaviour
{
    [System.Serializable]
    public class CrowdChunk
    {
        public Matrix4x4[] matrices;

        public float[] animIndices;

        public float[] timeOffsets;

        public Bounds bounds;
    }

    // RENDERING
    //-----------------------------------------

    [Header("Rendering")]
    public Mesh mesh;

    public Material material;

    // SCALE
    //-----------------------------------------

    [Header("Scale")]
    public Vector3 modelScale = Vector3.one;

    // CROWD SIZE
    //-----------------------------------------

    [Header("Crowd Size")]
    public int rows = 20;

    public int cols = 20;

    [Tooltip("Distance between people")]
    public float spacing = 1.5f;

    [Tooltip("How much each row rises upward")]
    public float rowHeight = 0.3f;

    // CHUNK SETTINGS
    //-----------------------------------------

    [Header("Chunk Settings")]

    [Tooltip("How many rows inside one chunk")]
    public int chunkRows = 4;

    [Tooltip("How many cols inside one chunk")]
    public int chunkCols = 4;

    // ANIMATION
    //-----------------------------------------

    [Header("Animation")]
    public int animationIndex = 0;

    // DEBUG
    //-----------------------------------------

    [Header("Debug")]
    public bool showChunkBounds = true;

    // CAMERA CULLING
    //-----------------------------------------

    [Header("Culling")]
    public Camera[] renderCameras;

    // INTERNAL
    //-----------------------------------------

    List<CrowdChunk> chunks =
        new List<CrowdChunk>();

    MaterialPropertyBlock mpb;

    // START
    //-----------------------------------------

    void Start()
    {
        // IMPORTANT FOR VAT
        // Slightly expand mesh bounds
        // so animation doesn't clip

        mesh.bounds = new Bounds(
            Vector3.zero,
            new Vector3(5f, 5f, 5f)
        );

        mpb = new MaterialPropertyBlock();

        GenerateCrowd();
    }

    // GENERATE CROWD
    //-----------------------------------------

    void GenerateCrowd()
    {
        chunks.Clear();

        // How many chunks across arena
        //---------------------------------

        int chunkGridRows =
            Mathf.CeilToInt(
                (float)rows / chunkRows
            );

        int chunkGridCols =
            Mathf.CeilToInt(
                (float)cols / chunkCols
            );

        // CREATE EACH CHUNK
        //---------------------------------

        for (int chunkR = 0;
            chunkR < chunkGridRows;
            chunkR++)
        {
            for (int chunkC = 0;
                chunkC < chunkGridCols;
                chunkC++)
            {
                CreateChunk(
                    chunkR,
                    chunkC
                );
            }
        }
    }

    // CREATE SINGLE CHUNK
    //-----------------------------------------

    void CreateChunk(
        int chunkRow,
        int chunkCol)
    {
        List<Matrix4x4> matrices =
            new List<Matrix4x4>();

        List<float> animIndices =
            new List<float>();

        List<float> timeOffsets =
            new List<float>();

        // Calculate chunk range
        //---------------------------------

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

        // Crowd centering
        //---------------------------------

        float centerOffset =
            (cols - 1) * 0.5f;

        // Bounds tracking
        //---------------------------------

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

        // Generate people inside chunk
        //---------------------------------

        for (int r = startRow;
            r < endRow;
            r++)
        {
            for (int c = startCol;
                c < endCol;
                c++)
            {
                // POSITION
                //-----------------------------

                Vector3 columnOffset =
                    Vector3.right *
                    ((c - centerOffset)
                    * spacing);

                Vector3 rowOffset =
                    Vector3.forward *
                    (r * spacing);

                Vector3 heightOffset =
                    Vector3.up *
                    ((rows - 1 - r)
                    * rowHeight);

                Vector3 localPosition =
                    columnOffset +
                    rowOffset +
                    heightOffset;

                Vector3 worldPosition =
                    transform.position +
                    (transform.rotation
                    * localPosition);

                // ROTATION
                //-----------------------------

                Quaternion randomRotation =
                    Quaternion.Euler(
                        0f,
                        Random.Range(-15f, 15f),
                        0f
                    );

                Quaternion finalRotation =
                    transform.rotation *
                    randomRotation;

                // MATRIX
                //-----------------------------

                Matrix4x4 matrix =
                    Matrix4x4.TRS(
                        worldPosition,
                        finalRotation,
                        modelScale
                    );

                matrices.Add(matrix);

                // VAT DATA
                //-----------------------------

                animIndices.Add(
                    animationIndex
                );

                timeOffsets.Add(
                    Random.Range(0f, 1f)
                );

                // BOUNDS
                //-----------------------------

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

        // CREATE CHUNK OBJECT
        //---------------------------------

        CrowdChunk chunk =
            new CrowdChunk();

        chunk.matrices =
            matrices.ToArray();

        chunk.animIndices =
            animIndices.ToArray();

        chunk.timeOffsets =
            timeOffsets.ToArray();

        // Add small padding
        //---------------------------------

        Vector3 padding =
            new Vector3(2f, 3f, 2f);

        chunk.bounds =
            new Bounds();

        chunk.bounds.SetMinMax(
            min - padding,
            max + padding
        );

        chunks.Add(chunk);
    }

    // UPDATE
    //-----------------------------------------

    void Update()
    {
        // CHANGE ANIMATION
        //---------------------------------

        if (Keyboard.current.digit1Key
            .wasPressedThisFrame)
        {
            foreach (CrowdChunk chunk
                in chunks)
            {
                for (int i = 0;
                    i < chunk.animIndices.Length;
                    i++)
                {
                    chunk.animIndices[i] = 0;
                }
            }
        }

        if (Keyboard.current.digit2Key
            .wasPressedThisFrame)
        {
            foreach (CrowdChunk chunk
                in chunks)
            {
                for (int i = 0;
                    i < chunk.animIndices.Length;
                    i++)
                {
                    chunk.animIndices[i] = 1;
                }
            }
        }

        // CAMERA FRUSTUM
        //---------------------------------

        foreach (CrowdChunk chunk in chunks)
        {
            bool visible = false;

            foreach (Camera cam in renderCameras)
            {
                if (cam == null)
                    continue;

                Plane[] planes = GeometryUtility.CalculateFrustumPlanes(cam);

                if (GeometryUtility.TestPlanesAABB(planes, chunk.bounds))
                {
                    visible = true;
                    break;
                }
            }

            if (!visible)
                continue;

            mpb.SetFloatArray("_AnimIndex", chunk.animIndices);
            mpb.SetFloatArray("_DT", chunk.timeOffsets);

            Graphics.DrawMeshInstanced(
                mesh,
                0,
                material,
                chunk.matrices,
                chunk.matrices.Length,
                mpb
            );
        }
    }

    // DEBUG DRAW
    //-----------------------------------------

    void OnDrawGizmos()
    {

        // DRAW OVERALL CROWD AREA
        // ----------------------------------

        float crowdWidth = (cols - 1) * spacing;
        float crowdDepth = (rows - 1) * spacing;
        float crowdHeight = (rows - 1) * rowHeight;

        Vector3 centerOffset = new Vector3(
            0f,
            crowdHeight * 0.5f,
            crowdDepth * 0.5f
        );

        Vector3 size = new Vector3(
            crowdWidth,
            crowdHeight + 2f,
            crowdDepth
        );

        Matrix4x4 oldMatrix = Gizmos.matrix;

        Gizmos.matrix = Matrix4x4.TRS(
            transform.position,
            transform.rotation,
            Vector3.one
        );

        // Yellow = entire crowd area
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(
            centerOffset,
            size
        );

        Gizmos.matrix = oldMatrix;

        // DRAW CHUNK BOUNDS
        // ----------------------------------

        if (!showChunkBounds)
            return;

        if (chunks == null)
            return;

        Gizmos.color = Color.green;

        foreach (CrowdChunk chunk in chunks)
        {
            Gizmos.DrawWireCube(
                chunk.bounds.center,
                chunk.bounds.size * 0.8f
            );
        }
    }
}
