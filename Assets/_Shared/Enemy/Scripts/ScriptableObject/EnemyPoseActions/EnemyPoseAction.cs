using UnityEngine;

namespace Enemy
{
    [CreateAssetMenu(fileName = "EnemyPoseAction", menuName = "Game/Enemy/Pose Action")]
    public class EnemyPoseAction : ScriptableObject
    {
        private static readonly int[] AnimatorTriggerHashes =
        {
            Animator.StringToHash("Idle"),
            Animator.StringToHash("Yaa"),
            Animator.StringToHash("Power"),
            Animator.StringToHash("Muscle")
        };

        [Header("Visual")]
        [SerializeField] private Color _preCommitColor = Color.white;
        [SerializeField] private Color _committedColor = Color.white;
        [SerializeField, Min(0.01f)] private float _preCommitBlinkInterval = 0.5f;
        [SerializeField, Min(0.01f)] private float _parryGraceBlinkInterval = 0.1f;

        [Header("Animation")]
        [SerializeField] private string _preCommitAnimatorTriggerName;
        [SerializeField] private string _committedAnimatorTriggerName;

        private bool _isCommitted;

        public bool IsCommitted => _isCommitted;

        private void OnEnable()
        {
            _isCommitted = false;
        }

        public void OnStart(Enemy enemy)
        {
            if (enemy == null) { return; }

            _isCommitted = false;
            ApplyVisual(enemy, _preCommitColor, _preCommitAnimatorTriggerName);
            ApplyAnimatorTrigger(enemy, _preCommitAnimatorTriggerName);
        }

        public void OnUpdate(Enemy enemy, float deltaTime)
        {
            if (enemy == null) { return; }

            switch (enemy.PoseState)
            {
                case EnemyPoseState.PreCommit:
                    ApplyBlink(enemy, _preCommitColor, _preCommitBlinkInterval);
                    break;
                case EnemyPoseState.ParryGrace:
                    ApplyBlink(enemy, _committedColor, _parryGraceBlinkInterval);
                    break;
                case EnemyPoseState.Completed:
                    ApplyColor(enemy, _committedColor);
                    break;
            }
        }

        public void OnCommitPose(Enemy enemy)
        {
            if (enemy == null) { return; }

            _isCommitted = true;
            ApplyVisual(enemy, _committedColor, _committedAnimatorTriggerName);
            if (_preCommitAnimatorTriggerName != _committedAnimatorTriggerName)
            {
                ApplyAnimatorTrigger(enemy, _committedAnimatorTriggerName);
            }
        }

        public void OnEnd(Enemy enemy)
        {
            if (enemy == null) { return; }

            _isCommitted = false;

            Proto_EnemyChangeColor visual = enemy.GetComponent<Proto_EnemyChangeColor>();
            if (visual != null)
            {
                visual.ResetColor();
            }

            ApplyAnimatorTrigger(enemy, "Idle");
        }

        private static void ApplyVisual(Enemy enemy, Color color, string animatorTriggerName)
        {
            ApplyColor(enemy, color);
        }

        private static void ApplyColor(Enemy enemy, Color color)
        {
            Proto_EnemyChangeColor visual = enemy.GetComponent<Proto_EnemyChangeColor>();
            if (visual != null)
            {
                visual.SetColor(color);
            }
        }

        private static void ApplyBlink(Enemy enemy, Color color, float blinkInterval)
        {
            float interval = Mathf.Max(0.01f, blinkInterval);
            bool usePoseColor = Mathf.FloorToInt(enemy.PoseStateElapsedTime / interval) % 2 == 0;
            ApplyColor(enemy, usePoseColor ? color : Color.white);
        }

        private static void ApplyAnimatorTrigger(Enemy enemy, string animatorTriggerName)
        {
            if (!string.IsNullOrEmpty(animatorTriggerName))
            {
                Animator animator = enemy?.GetAnimator;
                if (animator == null)
                {
                    Debug.LogWarning($"EnemyPoseAction could not set trigger '{animatorTriggerName}' because Animator was not found.", enemy);
                    return;
                }

                foreach (int triggerHash in AnimatorTriggerHashes)
                {
                    animator.ResetTrigger(triggerHash);
                }

                animator.SetTrigger(animatorTriggerName);
                Debug.Log("-----------------");
                Debug.Log(animatorTriggerName);
                Debug.Log("-----------------");
            }
        }
    }
}
