using Pose;
using AudienceRequest;
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
        [SerializeField] private string _audienceRequestContextName = "AudienceRequestContext";
        [SerializeField] private AudienceRequestContext _audienceRequestContext;
        [SerializeField] private FeverGaugeController _feverGaugeController;

        // ポーズ中かどうか
        private bool _isPaused = false;

        // ポーズ中のEnemy
        private Enemy _pausedEnemy;

        // AI
        private float _timer;
        private float _nextFeverContextResolveTime;

        private const float FeverContextResolveInterval = 0.5f;

        private void Start()
        {
            // EnemyContextからPoseParryJudgeを取得
            _poseParryJudge = _enemyContext?.GetPoseContext()?.GetPoseParryJudge;
            if (_poseParryJudge) { _poseParryJudge.OnParrySuccess += OnParrySuccess; }
            //Debug.Log("EnemyPoseController Start. _poseParryJudge = " + _poseParryJudge);

            _timer = 0f;
        }

        // Update is called once per frame
        private void Update()
        {
            if (InGameFlowLock.IsGameStartActionPlaying)
            {
                _timer = 0f;
                return;
            }

            if (_isPaused)
            {
                if (DebugShortcuts.Combo(Key.D, Key.P))
                {
                    _poseParryJudge?.DebugOnParry();
                }
            }
            else
            {
                if (IsFeverActive())
                {
                    _timer = 0f;
                    return;
                }

                if (UpdatePoseTimer())
                {
                    _pausedEnemy = _enemyRegistry.GetRandomEnemy();
                    if (_pausedEnemy == null) { return; }
                    _isPaused = true;
                    _timer = 0f;
                    _pausedEnemy.RandomPose();
                }

                if (DebugShortcuts.Combo(Key.E, Key.P))
                {
                    _pausedEnemy = _enemyRegistry.GetRandomEnemy();
                    if (_pausedEnemy == null) { return; }
                    _isPaused = true;
                    _timer = 0f;
                    _pausedEnemy.RandomPose();
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

        private bool IsFeverActive()
        {
            SetupFeverGaugeController();
            return _feverGaugeController != null && _feverGaugeController.IsFever;
        }

        private void SetupFeverGaugeController()
        {
            if (_feverGaugeController != null)
            {
                return;
            }

            if (_audienceRequestContext == null)
            {
                if (Time.time < _nextFeverContextResolveTime)
                {
                    return;
                }

                _nextFeverContextResolveTime = Time.time + FeverContextResolveInterval;

                ContextManager contextManager = ContextManager.Instance;
                if (contextManager != null && !string.IsNullOrWhiteSpace(_audienceRequestContextName))
                {
                    _audienceRequestContext = contextManager.Get<AudienceRequestContext>(_audienceRequestContextName);
                }

                if (_audienceRequestContext == null)
                {
                    _audienceRequestContext = FindFirstObjectByType<AudienceRequestContext>();
                }
            }

            _feverGaugeController = _audienceRequestContext?.GetFeverGaugeController();
        }
    }
}
