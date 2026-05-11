using UnityEngine;

namespace Enemy
{
    public abstract class EnemyPoseAction : ScriptableObject
    {
        public virtual void OnStart(Enemy enemy)
        {
        }

        public virtual void OnUpdate(Enemy enemy, float deltaTime)
        {
        }

        public virtual void OnEnd(Enemy enemy)
        {
        }
    }
}
