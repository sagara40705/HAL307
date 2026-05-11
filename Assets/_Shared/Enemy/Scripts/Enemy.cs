using UnityEngine;
using Pose;

namespace Enemy
{
    public class Enemy : MonoBehaviour
    {
        //　EnemyData
        private EnemyData _data;

        // 管理クラスの参照
        private EnemyRegistry _enemyRegistry;
        private EnemyPoseController _poseController;

        //Enemyの状態
        private bool _isPaused = false;
        private PoseID _currentPoseID;
        private float _poseTimer = 0f;
        private EnemyPoseData　_currentPoseData;

        private void Update()
        {
            if (_isPaused)
            {
                _poseTimer -= Time.deltaTime;
                if (_poseTimer <= 0f)
                {
                    OnUnpause();
                }

            }
        }

        // Enemyが生成されたときの初期化
        public void Initialize(EnemyRegistry enemyRegistry, EnemyPoseController poseController, EnemyData data)
        {
            // EnemyRegistryを受け取って登録する
            _enemyRegistry = enemyRegistry;
            _enemyRegistry.Register(this);

            // EnemyPoseControllerを受け取る
            _poseController = poseController;

            // EnemyDataを受け取る
            _data = data;
        }

        // Pose終了時の共通処理
        private void OnUnpause()
        {
            _isPaused = false;
            // ここにPose終了時の共通処理を記述する
            Debug.Log($"Enemy {name} unpaused.");
            _currentPoseData.PoseAction.OnEnd(this);

            _poseController.Unpause();
            _currentPoseID = PoseID.None;
            _poseTimer = 0f;
            _currentPoseData = null;
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

        public void RandomPose()
        {
            if (_data == null || _data.PoseSet.Poses.Count == 0)
            {
                Debug.LogWarning($"Enemy {name} has no poses defined in its data.");
                return;
            }
            EnemyPoseData randomPoseData = _data.PoseSet.GetRandomPose();
            Pause(randomPoseData);
        }

        public void Pause(EnemyPoseData poseData)
        {
            _currentPoseID = poseData.ID;
            _isPaused = true;
            _poseTimer = poseData.Duration;
            _currentPoseData = poseData;

            Debug.Log($"Enemy {name} paused with pose {_currentPoseID}.");
            _currentPoseData.PoseAction.OnStart(this);
        }
    }
}
