using System.Collections;
using UnityEngine;

public class AnimationTest : MonoBehaviour
{
    private Animator animator;

    private readonly string[] poses =
    {
        "Idle",
        "Power",
        "Yaa",
        "Muscle"
    };

    private int lastPose = -1;

    private void Start()
    {
        animator = GetComponent<Animator>();
        StartCoroutine(RandomPoseLoop());
    }

    private IEnumerator RandomPoseLoop()
    {
        while (true)
        {
            // Pick a random pose that isn't the same as the last one
            int index;
            do
            {
                index = Random.Range(0, poses.Length);
            }
            while (index == lastPose);

            lastPose = index;

            string pose = poses[index];
            Debug.Log("Playing: " + pose);

            animator.SetTrigger(pose);

            // Wait one frame for the Animator to change states
            yield return null;

            // Wait until the transition is finished
            while (animator.IsInTransition(0))
            {
                yield return null;
            }

            // Wait until the current animation has finished
            while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
            {
                yield return null;
            }
        }
    }
}
