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
            0.5f,  // 遷移時間（秒）
            0,     // レイヤー
            0f     // Idleを先頭から再生する
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

    // エンドレスではカウントダウン中も既存の登場ポーズを維持する。
    // ゲーム用Idleには歩行が含まれるため、カウントダウン完了による解除後に遷移する。
    public void SetCountdownHold(bool held)
    {
        if (!TryResolveAnimator()) { return; }
        if (_countdownHeld == held) { return; }
        _countdownHeld = held;
        if (held)
        {
            _countdownRootMotion = _animator.applyRootMotion;
            _animator.applyRootMotion = false;
            // 生成時に選択したポーズを維持する。参加NPCの着地からのブレンドも含む。
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
