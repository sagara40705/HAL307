using UnityEngine;
using Pose;

namespace Enemy
{
    public class Enemy : MonoBehaviour
    {
        // 管理クラスの参照
        private EnemyRegistry _enemyRegistry;
        private EnemyPoseController _poseController;

        //Enemyの状態
        private bool _isPaused = false;
        private PoseID _currentPoseID;

        // Enemyが生成されたときの初期化
        public void Initialize(EnemyRegistry enemyRegistry, EnemyPoseController poseController)
        {
            // EnemyRegistryを受け取って登録する
            _enemyRegistry = enemyRegistry;
            _enemyRegistry.Register(this);

            // EnemyPoseControllerを受け取る
            _poseController = poseController;
        }

        public void Pause(PoseID poseID)
        {
            _currentPoseID = poseID;
            _isPaused = true;
            Debug.Log($"Enemy {name} paused with pose {poseID}.");
        }

        // Pose終了時の共通処理
        private void OnUnpause()
        {
            // ここにPose終了時の共通処理を記述する
            Debug.Log($"Enemy {name} unpaused.");
            _poseController.Unpause();
        }

        // Enemyが破壊されたときのクリーンアップ
        private void OnDestroy()
        {
            // EnemyRegistryから登録解除する
            if (_enemyRegistry != null)
            {
                _enemyRegistry.Unregister(this);
            }
        }
    }
}
