using UnityEngine;
using UnityEngine.InputSystem;
public class TestVAT : MonoBehaviour
{
    Renderer r;
    MaterialPropertyBlock mpb;

    void Start()
    {
        r = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();
    }

    void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
            SetAnim(0);

        if (Keyboard.current.rKey.wasPressedThisFrame)
            SetAnim(1);

    }

    void SetAnim(int index)
    {
        r.GetPropertyBlock(mpb);
        mpb.SetFloat("_AnimIndex", index);
        r.SetPropertyBlock(mpb);

        Debug.Log("Animation set to: " + index);
    }
}
