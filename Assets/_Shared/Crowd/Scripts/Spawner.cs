using UnityEngine;
using UnityEngine.InputSystem;

public class CrowdSpawner : MonoBehaviour
{
    [Header("Rendering")]
    public Mesh mesh;
    public Material material;
    public ComputeShader cullingShader;

    [Header("Scale")]
    public Vector3 modelScale = Vector3.one;

    [Header("Spawn Area")]
    public int rows = 5;
    public int cols = 10;

    [Tooltip("How much each row rises upward")]
    public float rowHeight = 0.3f;

    [Tooltip("Distance between people")]
    public float spacing = 1.5f;

    [Header("Animation")]
    public int animationIndex = 0;

    // Instance data
    Matrix4x4[] matrices;

    // VAT data
    float[] animIndices;
    float[] timeOffsets;

    MaterialPropertyBlock mpb;

    void Start()
    {
        int count = rows * cols;

        matrices = new Matrix4x4[count];

        animIndices = new float[count];
        timeOffsets = new float[count];

        mpb = new MaterialPropertyBlock();

        GenerateCrowd();
    }

    void GenerateCrowd()
    {
        int index = 0;

        // center crowd nicely
        float centerOffset = (cols - 1) * 0.5f;

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                // LOCAL POSITION
                //-------------------------------------------------

                // spread sideways
                Vector3 columnOffset =
                    Vector3.right *
                    ((c - centerOffset) * spacing);

                // move rows forward
                Vector3 rowOffset =
                    Vector3.forward *
                    (r * spacing);

                // raise each next row
                Vector3 heightOffset =
                    Vector3.up *
                    ((rows - 1 - r) * rowHeight);

                // combine local offsets
                Vector3 localPosition =
                    columnOffset +
                    rowOffset +
                    heightOffset;

                // CONVERT TO WORLD POSITION
                //-------------------------------------------------

                Vector3 worldPosition =
                    transform.position +
                    (transform.rotation * localPosition);

                // ROTATION
                //-------------------------------------------------

                // small random crowd variation
                Quaternion randomRotation =
                    Quaternion.Euler(
                        0f,
                        Random.Range(-15f, 15f),
                        0f
                    );

                Quaternion finalRotation =
                    transform.rotation *
                    randomRotation;

                // SAVE MATRIX
                //-------------------------------------------------

                matrices[index] =
                    Matrix4x4.TRS(
                        worldPosition,
                        finalRotation,
                        modelScale
                    );

                // VAT DATA
                //-------------------------------------------------

                animIndices[index] = animationIndex;

                timeOffsets[index] =
                    Random.Range(0f, 1f);

                index++;
            }
        }
    }
    // GIZMO PREVIEW
    //-------------------------------------------------

    [Header("Debug")]
    public bool showPreview = true;

    void OnDrawGizmos()
    {
        if (!showPreview)
            return;

        Gizmos.color = Color.cyan;

        // crowd size
        float width = cols * spacing;
        float length = rows * spacing;
        float heightSize = rows * rowHeight;

        // center position
        Vector3 center =
            transform.position +
            transform.rotation *
            new Vector3(
                0f,
                heightSize * 0.5f,
                length * 0.5f
            );

        // rotate gizmo with object
        Matrix4x4 oldMatrix = Gizmos.matrix;

        Gizmos.matrix =
            Matrix4x4.TRS(
                transform.position,
                transform.rotation,
                Vector3.one
            );

        // draw wire cube
        Gizmos.DrawWireCube(
            new Vector3(
                0f,
                heightSize * 0.5f,
                length * 0.5f
            ),
            new Vector3(
                width,
                heightSize,
                length
            )
        );

        Gizmos.matrix = oldMatrix;
    }

    void Update()
    {
        // CHANGE ANIMATION
        //-------------------------------------------------

        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            for (int i = 0; i < animIndices.Length; i++)
            {
                animIndices[i] = 0;
            }
        }

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            for (int i = 0; i < animIndices.Length; i++)
            {
                animIndices[i] = 1;
            }
        }

        // SEND VAT DATA
        //-------------------------------------------------

        mpb.SetFloatArray("_AnimIndex", animIndices);
        mpb.SetFloatArray("_DT", timeOffsets);

        // DRAW CROWD
        //-------------------------------------------------

        Graphics.DrawMeshInstanced(
            mesh,
            0,
            material,
            matrices,
            matrices.Length,
            mpb
        );
    }
}
