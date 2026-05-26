using UnityEngine;
using Pose;
using RuntimeDebug;

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
        private EnemyPoseData _currentPoseData;

        // パリイ関連
        private bool _isParried = false;
        private float _parryTimer = 0f;

        //
        Proto_EnemyChangeColor _colorChanger;

        void Start()
        {
            _colorChanger = GetComponent<Proto_EnemyChangeColor>();
            if (_colorChanger == null)
            {
                Debug.LogWarning($"Enemy {name} does not have a Proto_EnemyChangeColor component.");
            }

            // デバッグ用のUIを作成
            DebugWindow window = RuntimeDebug.DebugMenu.CreateWindow(name);
            window.CreateMessage("Current Pose", () => _currentPoseID.ToString());
            window.CreateMessage("Pose Timer", () => _poseTimer.ToString("F2"));
            window.CreateMessage("Parried", () => _isParried.ToString());
            window.CreateMessage("Parry Timer", () => _parryTimer.ToString("F2"));
        }

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

            if (_isParried)
            {
                _parryTimer -= Time.deltaTime;
                if (_parryTimer <= 0f)
                {
                    _isParried = false;
                    _colorChanger?.ResetColor();
                    Debug.Log($"Enemy {name} parry state ended.");
                }
            }
        }

        // Enemyが生成されたときの初期化
        public void Initialize(EnemyRegistry enemyRegistry, EnemyPoseController poseController, EnemyData data)
        {
            // EnemyRegistryを受け取って登録する
            _enemyRegistry = enemyRegistry;
            _enemyRegistry?.Register(this);

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
            if (_currentPoseData) { _currentPoseData.PoseAction.OnEnd(this); }

            _poseController?.Unpause();
            _currentPoseID = PoseID.None;
            _poseTimer = 0f;
            _currentPoseData = null;
        }

        // Enemyが破壊されたときのクリーンアップ
        private void OnDestroy()
        {
            // EnemyRegistryから登録解除する
            _enemyRegistry?.Unregister(this);
            RuntimeDebug.DebugMenu.DestroyWindow(name);
        }

        public PoseID RandomPose()
        {
            if (_data == null || _data.PoseSet.Poses.Count == 0)
            {
                Debug.LogWarning($"Enemy {name} has no poses defined in its data.");
                return PoseID.None;
            }
            EnemyPoseData randomPoseData = _data.PoseSet.GetRandomPose();
            Pause(randomPoseData);
            return randomPoseData.ID;
        }

        public void Pause(EnemyPoseData poseData)
        {
            _currentPoseID = poseData.ID;
            _isPaused = true;
            _poseTimer = poseData.Duration;
            _currentPoseData = poseData;

            Debug.Log($"Enemy {name} paused with pose {_currentPoseID}.");
            _currentPoseData?.PoseAction?.OnStart(this);
        }

        //　パリイされたときの処理
        public void OnParrySuccess()
        {
            Debug.Log($"Enemy {name} was parried with pose {_currentPoseID}.");
            OnUnpause();
            _colorChanger?.SetColor(Color.black);
            _isParried = true;
            _parryTimer = 3.0f;
        }
    }
}
