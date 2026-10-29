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
    private bool _countdownHeld;
    private bool _countdownRootMotion;

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

        _animator.CrossFadeInFixedTime(
            IdleStateHash,
            0.5f,  // Transition duration (seconds)
            0,     // Layer
            0f     // Start Idle from beginning
        );
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

    public void SetAnimationSpeed(float speed)
    {
        animationSpeed = speed;
    }

    public float GetAnimationSpeed()
    {
        return animationSpeed;
    }

    // Endless keeps the existing entrance pose through the countdown. Gameplay Idle
    // contains walking, so enter it only when the countdown completion releases us.
    public void SetCountdownHold(bool held)
    {
        if (!TryResolveAnimator()) { return; }
        if (_countdownHeld == held) { return; }
        _countdownHeld = held;
        if (held)
        {
            _countdownRootMotion = _animator.applyRootMotion;
            _animator.applyRootMotion = false;
            // Keep the pose selected at spawn, including the incoming NPC's blended landing.
            int poseIndex = _animator.GetInteger("StartPoseIndex");
            int poseState = Animator.StringToHash("Base Layer.StartPose.START" + (char)('A' + poseIndex));
            ResetAnimationTriggers();
            _animator.ResetTrigger("StartPose");
            _animator.Play(poseState, 0, 0f);
            _animator.Update(0f);
        }
        else
        {
            _animator.applyRootMotion = _countdownRootMotion;
            ResetAnimationTriggers();
            _animator.Play(IdleStateHash, 0, 0f);
        }
        _animator.speed = animationSpeed;
    }
}
