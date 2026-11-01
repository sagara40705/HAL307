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
            // 前回と異なるポーズをランダムに選ぶ
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

            // Animatorのステートが切り替わるまで1フレーム待つ
            yield return null;

            // ステート間の遷移が完了するまで待つ
            while (animator.IsInTransition(0))
            {
                yield return null;
            }

            // 現在のアニメーションが完了するまで待つ
            while (animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.0f)
            {
                yield return null;
            }
        }
    }
}
