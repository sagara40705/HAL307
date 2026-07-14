using UnityEngine;

public class OnStartPose : MonoBehaviour
{
    private Animator _animator;

    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        RandomPose();
    }

    public void RandomPose()
    {
        int randomPose = Random.Range(0, 4);

        _animator.SetInteger("StartPoseIndex", randomPose);
    }

    public void SetTriggerIdle()
    {
        _animator.SetTrigger("Idle");
    }
}
