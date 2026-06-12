using UnityEngine;

namespace Enemy
{
    [CreateAssetMenu(fileName = "EnemyPoseAction", menuName = "Game/Enemy/Pose Action")]
    public class EnemyPoseAction : ScriptableObject
    {
        [Header("Visual")]
        [SerializeField] private Color _preCommitColor = Color.white;
        [SerializeField] private Color _committedColor = Color.white;

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
            Proto_EnemyChangeColor visual = enemy.GetComponent<Proto_EnemyChangeColor>();
            if (visual != null)
            {
                visual.SetColor(color);
            }

            if (!string.IsNullOrEmpty(animatorTriggerName))
            {
                Animator animator = enemy.GetComponentInChildren<Animator>();
                animator?.SetTrigger(animatorTriggerName);
            }
        }
    }
}
