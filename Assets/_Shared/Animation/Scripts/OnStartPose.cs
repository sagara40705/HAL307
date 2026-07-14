using UnityEngine;

public class OnStartPose : MonoBehaviour
{
    private static readonly int IdleStateHash = Animator.StringToHash("Idle");
    private static readonly int[] TriggerHashes =
    {
        Animator.StringToHash("Idle"),
        Animator.StringToHash("Yaa"),
        Animator.StringToHash("Power"),
        Animator.StringToHash("Muscle")
    };

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
        if (!TryResolveAnimator())
        {
            return;
        }

        int randomPose = Random.Range(0, 4);

        _animator.SetInteger("StartPoseIndex", randomPose);
    }

    public void SetTriggerIdle()
    {
        if (!TryResolveAnimator())
        {
            return;
        }

        foreach (int triggerHash in TriggerHashes)
        {
            _animator.ResetTrigger(triggerHash);
        }

        _animator.Play(IdleStateHash, 0, 0f);
    }

    private bool TryResolveAnimator()
    {
        if (_animator == null)
        {
            _animator = GetComponent<Animator>();
        }

        if (_animator != null)
        {
            return true;
        }

        Debug.LogWarning("[OnStartPose] Animator was not found.", this);
        return false;
    }
}
