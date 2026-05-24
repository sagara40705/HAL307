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

        // AI
        private float _timer;

        private void Start()
        {
            // EnemyContextからPoseParryJudgeを取得
            _poseParryJudge = _enemyContext?.GetPoseContext()?.GetPoseParryJudge;
            if (_poseParryJudge) { _poseParryJudge.OnParrySuccess += OnParrySuccess; }
            Debug.Log("EnemyPoseController Start. _poseParryJudge = " + _poseParryJudge);

            _timer = 0f;
        }

        // Update is called once per frame
        private void Update()
        {
            if (_isPaused)
            {
                if (DebugShortcuts.Combo(Key.D, Key.P))
                {
                    _poseParryJudge?.DebugOnParry();
                }
            }
            else
            {
                if (UpdatePoseTimer())
                {
                    _pausedEnemy = _enemyRegistry.GetRandomEnemy();
                    _isPaused = true;
                    _timer = 0f;
                    PoseID pose = _pausedEnemy.RandomPose();
                    if (_poseParryJudge == null) { return; }
                    _poseParryJudge?.StartEnemyPose(pose);
                }

                if (DebugShortcuts.Combo(Key.E, Key.P))
                {
                    _pausedEnemy = _enemyRegistry.GetRandomEnemy();
                    _isPaused = true;
                    _timer = 0f;
                    PoseID pose = _pausedEnemy.RandomPose();
                    if (_poseParryJudge == null) { return; }
                    _poseParryJudge?.StartEnemyPose(pose);
                }
            }
        }

        // ポーズ状態を解除する
        public void Unpause()
        {
            _pausedEnemy = null;
            _isPaused = false;
            if (_poseParryJudge == null) { return; }
            _poseParryJudge?.EndEnemyPose();
        }

        // パリイ成功時の処理
        public void OnParrySuccess()
        {
            _pausedEnemy?.OnParrySuccess();
        }

        private bool UpdatePoseTimer()
        {
            if (_isPaused) { return false; }

            _timer += Time.deltaTime;
            if (_timer >= 5.0f)
            {
                return true;
            }

            return false;
        }
    }
}
