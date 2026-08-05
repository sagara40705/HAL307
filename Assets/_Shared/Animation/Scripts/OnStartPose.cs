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
    private static readonly int[] PoseTriggerHashes =
    {
        Animator.StringToHash("Yaa"),
        Animator.StringToHash("Power"),
        Animator.StringToHash("Muscle")
    };

    private Animator _animator;
    [Header("Animation")]
    [SerializeField] private float animationSpeed = 1.0f;

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

        ResetAnimationTriggers();
        _animator.Play(IdleStateHash, 0, 0f);
    }

    public void PlayRandomPoseAnimation()
    {
        if (!TryResolveAnimator())
        {
            return;
        }

        ResetAnimationTriggers();
        int randomIndex = Random.Range(0, PoseTriggerHashes.Length);
        _animator.SetTrigger(PoseTriggerHashes[randomIndex]);
    }

    private void ResetAnimationTriggers()
    {
        foreach (int triggerHash in TriggerHashes)
        {
            _animator.ResetTrigger(triggerHash);
        }
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

    private void Update()
    {
        if (_animator != null)
        {
            _animator.speed = animationSpeed;
        }
    }
}
