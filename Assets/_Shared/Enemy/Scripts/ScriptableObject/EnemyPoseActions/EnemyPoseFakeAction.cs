using UnityEngine;

namespace Enemy
{
    [CreateAssetMenu(fileName = "EnemyPoseFakeAction", menuName = "Game/Enemy/Pose Action/Fake")]
    public class EnemyPoseFakeAction : EnemyPoseAction
    {

        public override void OnStart(Enemy enemy)
        {
        }

        public override void OnUpdate(Enemy enemy, float deltaTime)
        {
        }

        public override void OnEnd(Enemy enemy)
        {
        }
    }
}
