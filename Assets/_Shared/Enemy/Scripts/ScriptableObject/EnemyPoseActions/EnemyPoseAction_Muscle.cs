using UnityEngine;

namespace Enemy
{
    [CreateAssetMenu(fileName = "EnemyPoseAction_Muscle", menuName = "Game/Enemy/Pose Action/Muscle")]
    public class EnemyPoseAction_Muscle : EnemyPoseAction
    {
        [SerializeField] private Color poseColor = Color.black;

        public override void OnStart(Enemy enemy)
        {
            if (enemy == null) { return; }

            Proto_EnemyChangeColor visual = enemy.GetComponent<Proto_EnemyChangeColor>();
            if (visual != null)
            {
                visual.SetColor(poseColor);
                Debug.Log("Muscleポーズ開始");
            }
        }

        public override void OnUpdate(Enemy enemy, float deltaTime)
        {
            // Muscleポーズ中の処理
        }

        public override void OnEnd(Enemy enemy)
        {
            Debug.Log("Muscleポーズ終了");
            if (enemy == null) { return; }

            Proto_EnemyChangeColor visual = enemy.GetComponent<Proto_EnemyChangeColor>();
            if (visual != null)
            {
                visual.ResetColor();
            }
        }
    }
}
