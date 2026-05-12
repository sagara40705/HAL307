using UnityEngine;

namespace Enemy
{
    [CreateAssetMenu(fileName = "EnemyPoseAction_Power", menuName = "Game/Enemy/Pose Action/Power")]
    public class EnemyPoseAction_Power : EnemyPoseAction
    {
        [SerializeField] private Color poseColor = Color.yellow;

        public override void OnStart(Enemy enemy)
        {
            if (enemy == null) { return; }

            Proto_EnemyChangeColor visual = enemy.GetComponent<Proto_EnemyChangeColor>();
            if (visual != null)
            {
                visual.SetColor(poseColor);
                Debug.Log("Powerポーズ開始");
            }
        }

        public override void OnUpdate(Enemy enemy, float deltaTime)
        {
            // Powerポーズ中の処理
        }

        public override void OnEnd(Enemy enemy)
        {
            Debug.Log("Powerポーズ終了");
            if (enemy == null) { return; }

            Proto_EnemyChangeColor visual = enemy.GetComponent<Proto_EnemyChangeColor>();
            if (visual != null)
            {
                visual.ResetColor();
            }
        }
    }
}
