using UnityEngine;
using UnityEngine.InputSystem;

public class NewMonoBehaviourScript : MonoBehaviour
{

    // このオブジェクトに付いているRenderer
    private Renderer targetRenderer;

    // 実行中に操作する専用Material
    private Material runtimeMaterial;

    // Shaderの「_BreakAmount」という名前をIDへ変換する。
    private static readonly int BreakAmountId =
        Shader.PropertyToID("_BreakAmount");

    // Update is called once per frame
    void Update()
    {
        
    }
}
