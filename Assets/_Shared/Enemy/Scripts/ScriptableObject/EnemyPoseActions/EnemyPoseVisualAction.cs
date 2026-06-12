using UnityEngine;

namespace Enemy
{
    [CreateAssetMenu(fileName = "EnemyPoseVisualAction", menuName = "Game/Enemy/Pose Action/Visual")]
    public class EnemyPoseVisualAction : EnemyPoseAction
    {
        [SerializeField] private Color _poseColor = Color.white;
        [SerializeField] private string _poseName = "Pose";
        [SerializeField] private string _animatorTriggerName;

        public override void OnStart(Enemy enemy)
        {
            if (enemy == null) { return; }

            Proto_EnemyChangeColor visual = enemy.GetComponent<Proto_EnemyChangeColor>();
            if (visual != null)
            {
                visual.SetColor(_poseColor);
            }

            if (!string.IsNullOrEmpty(_poseName))
            {
                Debug.Log($"[EnemyPoseVisualAction]{_poseName} pose start");
                Debug.Log($"[EnemyPoseVisualAction]{_poseColor.ToString()} change color");
            }
        }

        public override void OnUpdate(Enemy enemy, float deltaTime)
        {
        }

        public override void OnEnd(Enemy enemy)
        {
            if (enemy == null) { return; }

            Proto_EnemyChangeColor visual = enemy.GetComponent<Proto_EnemyChangeColor>();
            if (visual != null)
            {
                visual.ResetColor();
            }

            if (!string.IsNullOrEmpty(_poseName))
            {
                Debug.Log($"[EnemyPoseVisualAction]{_poseName} pose end and color reset");
            }
        }
    }
}
