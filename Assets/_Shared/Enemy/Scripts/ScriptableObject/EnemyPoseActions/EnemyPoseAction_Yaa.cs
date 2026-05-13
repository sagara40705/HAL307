using UnityEngine;

namespace Enemy
{
    [CreateAssetMenu(fileName = "EnemyPoseAction_Yaa", menuName = "Game/Enemy/Pose Action/Yaa")]
    public class EnemyPoseAction_Yaa : EnemyPoseAction
    {
        [SerializeField] private Color poseColor = Color.green;

        public override void OnStart(Enemy enemy)
        {

            if (enemy == null) { return; }

            Proto_EnemyChangeColor visual = enemy.GetComponent<Proto_EnemyChangeColor>();
            if (visual != null)
            {
                visual.SetColor(poseColor);
                Debug.Log("Yaaポーズ開始");
            }
        }

        public override void OnUpdate(Enemy enemy, float deltaTime)
        {
            // Yaaポーズ中の処理
        }

        public override void OnEnd(Enemy enemy)
        {
            Debug.Log("Yaaポーズ終了");
            if (enemy == null) { return; }

            Proto_EnemyChangeColor visual = enemy.GetComponent<Proto_EnemyChangeColor>();
            if (visual != null)
            {
                visual.ResetColor();
            }
        }
    }
}
