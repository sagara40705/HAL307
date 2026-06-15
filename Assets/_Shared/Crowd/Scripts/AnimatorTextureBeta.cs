using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AnimatorTextureBaker : MonoBehaviour
{
    public ComputeShader infoTexGen;
    public int framesPerAnim = 60;

    void Start()
    {
        StartCoroutine(BakeAnimation());
    }

    IEnumerator BakeAnimation()
    {
        var animator = GetComponent<Animator>();
        var clips = animator.runtimeAnimatorController.animationClips;
        var skin = GetComponentInChildren<SkinnedMeshRenderer>();

        int vCount = skin.sharedMesh.vertexCount;
        int texWidth = Mathf.NextPowerOfTwo(vCount);
        int totalFrames = framesPerAnim * clips.Length;

        var mesh = new Mesh();
        animator.speed = 0;

        // ? CREATE ONE TEXTURE
        var pRt = new RenderTexture(texWidth, totalFrames, 0, RenderTextureFormat.ARGBHalf);
        var nRt = new RenderTexture(texWidth, totalFrames, 0, RenderTextureFormat.ARGBHalf);

        pRt.enableRandomWrite = true;
        nRt.enableRandomWrite = true;

        pRt.Create();
        nRt.Create();

        RenderTexture.active = pRt;
        GL.Clear(true, true, Color.clear);

        RenderTexture.active = nRt;
        GL.Clear(true, true, Color.clear);

        int frameOffset = 0;

        foreach (var clip in clips)
        {
            Debug.Log("Baking: " + clip.name);

            var info = new List<VertInfo>();

            animator.Play(clip.name, 0, 0);
            animator.Update(0);
            yield return null;

            for (int i = 0; i < framesPerAnim; i++)
            {
                float t = (float)i / (framesPerAnim - 1);

                animator.Play(clip.name, 0, t);
                animator.Update(0);
                yield return null;

                skin.BakeMesh(mesh);

                var verts = mesh.vertices;
                var norms = mesh.normals;

                for (int v = 0; v < vCount; v++)
                {
                    info.Add(new VertInfo()
                    {
                        position = verts[v],
                        normal = norms[v]
                    });
                }
            }

            var buffer = new ComputeBuffer(info.Count, System.Runtime.InteropServices.Marshal.SizeOf(typeof(VertInfo)));
            buffer.SetData(info);

            var kernel = infoTexGen.FindKernel("CSMain");

            infoTexGen.SetInt("VertCount", vCount);
            infoTexGen.SetInt("FrameOffset", frameOffset); // ?? KEY
            infoTexGen.SetBuffer(kernel, "meshInfo", buffer);
            infoTexGen.SetTexture(kernel, "OutPosition", pRt);
            infoTexGen.SetTexture(kernel, "OutNormal", nRt);

            uint x, y, z;
            infoTexGen.GetKernelThreadGroupSizes(kernel, out x, out y, out z);

            infoTexGen.Dispatch(
                kernel,
                Mathf.CeilToInt(vCount / (float)x),
                Mathf.CeilToInt(framesPerAnim / (float)y),
                1
            );

            buffer.Release();

            frameOffset += framesPerAnim; // ?? STACK HERE
        }

#if UNITY_EDITOR
        var posTex = Convert(pRt);
        var normTex = Convert(nRt);

        AssetDatabase.CreateAsset(posTex, "Assets/Combined_PosTex.asset");
        AssetDatabase.CreateAsset(normTex, "Assets/Combined_NmlTex.asset");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
#endif

        Debug.Log("Bake complete");
    }

    public Texture2D Convert(RenderTexture rt)
    {
        var texture = new Texture2D(rt.width, rt.height, TextureFormat.RGBAHalf, false);
        RenderTexture.active = rt;
        texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        texture.Apply();
        RenderTexture.active = null;
        return texture;
    }

    public struct VertInfo
    {
        public Vector3 position;
        public Vector3 normal;
    }
}