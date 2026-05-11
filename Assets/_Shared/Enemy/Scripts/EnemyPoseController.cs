using Pose;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Enemy
{
    public class EnemyPoseController : MonoBehaviour
    {
        // 参照
        [SerializeField] private EnemyRegistry _enemyRegistry;
        [SerializeField] private EnemyContext _enemyContext;
        [SerializeField] private PoseParryJudge _poseParryJudge;

        // ポーズ中かどうか
        private bool _isPaused = false;

        // ポーズ中のEnemy
        private Enemy _pausedEnemy;

        void Start()
        {
            // EnemyContextからPoseParryJudgeを取得
            _poseParryJudge = _enemyContext.PoseParryJudge;
            _poseParryJudge.OnParrySuccess += OnParrySuccess;
        }

        // Update is called once per frame
        private void Update()
        {
            if (_isPaused)
            {
                if (DebugShortcuts.Combo(Key.D, Key.P))
                {
                    _poseParryJudge.DebugOnParry();
                }
            }
            else
            {
                if (DebugShortcuts.Combo(Key.E, Key.P))
                {
                    _pausedEnemy = _enemyRegistry.GetRandomEnemy();
                    _isPaused = true;
                    _pausedEnemy.RandomPose();
                    Debug.Log($"Paused enemy: {_pausedEnemy?.name ?? "None"}");
                }
            }
        }

        // ポーズ状態を解除する
        public void Unpause()
        {
            _pausedEnemy = null;
            _isPaused = false;
        }

        // パリイ成功時の処理
        public void OnParrySuccess()
        {
            Debug.Log("Parry success received in EnemyPoseController.");

            if (_pausedEnemy != null)
            {
                Debug.Log($"Unpausing enemy: {_pausedEnemy.name}");
                _pausedEnemy.OnParrySuccess();
            }
        }
    }
}
