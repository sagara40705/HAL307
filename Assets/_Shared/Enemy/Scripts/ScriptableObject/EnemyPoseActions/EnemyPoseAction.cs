using UnityEngine;

namespace Enemy
{
    [CreateAssetMenu(fileName = "EnemyPoseAction", menuName = "Game/Enemy/Pose Action")]
    public class EnemyPoseAction : ScriptableObject
    {
        [Header("Visual")]
        [SerializeField] private Color _preCommitColor = Color.white;
        [SerializeField] private Color _committedColor = Color.white;
        [SerializeField, Min(0.01f)] private float _preCommitBlinkInterval = 0.2f;

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
        }

        public void OnUpdate(Enemy enemy, float deltaTime)
        {
            if (enemy == null) { return; }
            if (_isCommitted) { return; }

            float blinkInterval = Mathf.Max(0.01f, _preCommitBlinkInterval);
            bool usePoseColor = Mathf.FloorToInt(Time.time / blinkInterval) % 2 == 0;
            ApplyColor(enemy, usePoseColor ? _preCommitColor : Color.white);
        }

        public void OnCommitPose(Enemy enemy)
        {
            if (enemy == null) { return; }

            _isCommitted = true;
            ApplyVisual(enemy, _committedColor, _committedAnimatorTriggerName);
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
        }

        private static void ApplyVisual(Enemy enemy, Color color, string animatorTriggerName)
        {
            ApplyColor(enemy, color);
            ApplyAnimatorTrigger(enemy, animatorTriggerName);
        }

        private static void ApplyColor(Enemy enemy, Color color)
        {
            Proto_EnemyChangeColor visual = enemy.GetComponent<Proto_EnemyChangeColor>();
            if (visual != null)
            {
                visual.SetColor(color);
            }
        }

        private static void ApplyAnimatorTrigger(Enemy enemy, string animatorTriggerName)
        {
            if (!string.IsNullOrEmpty(animatorTriggerName))
            {
                Animator animator = enemy.GetComponentInChildren<Animator>();
                animator?.SetTrigger(animatorTriggerName);
            }
        }
    }
}
