using UnityEngine;
using UnityEngine.InputSystem;

public class AnimationTest : MonoBehaviour
{
    private Animator animator;

    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (Keyboard.current.aKey.wasPressedThisFrame)
        {
            Debug.Log("IDLE");
            animator.SetTrigger("Idle");
        }

        if (Keyboard.current.sKey.wasPressedThisFrame)
        {
            Debug.Log("POWER");
            animator.SetTrigger("Power");
        }

        if (Keyboard.current.dKey.wasPressedThisFrame)
        {
            Debug.Log("YAA");
            animator.SetTrigger("Yaa");
        }

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            Debug.Log("MUSCLE");
            animator.SetTrigger("Muscle");
        }
    }
}
