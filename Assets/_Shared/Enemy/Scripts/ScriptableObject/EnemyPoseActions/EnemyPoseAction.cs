using UnityEngine;

namespace Enemy
{
    public abstract class EnemyPoseAction : ScriptableObject
    {
        // ポーズアクションの開始時に呼び出されるメソッド
        public virtual void OnStart(Enemy enemy)
        {
        }

        // ポーズアクションの更新時に呼び出されるメソッド
        public virtual void OnUpdate(Enemy enemy, float deltaTime)
        {
        }

        // ポーズアクションの終了時に呼び出されるメソッド
        public virtual void OnEnd(Enemy enemy)
        {
        }

        // ポーズ変更のトリガーが発生したときに呼び出されるメソッド
        public virtual void PoseChangeTirgger(Enemy enemy)
        {

        }
    }
}
