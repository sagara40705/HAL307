using Pose;
using AudienceRequest;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Enemy
{
    public class EnemyPoseController : MonoBehaviour
    {
        private const string DebugWindowName = "Enemy Pose";

        // 参照
        [SerializeField] private EnemyRegistry _enemyRegistry;
        [SerializeField] private EnemyContext _enemyContext;
        [SerializeField] private PoseParryJudge _poseParryJudge;
        [SerializeField] private string _audienceRequestContextName = "AudienceRequestContext";
        [SerializeField] private AudienceRequestContext _audienceRequestContext;
        [SerializeField] private AudienceRequestSystem _audienceRequestSystem;
        [SerializeField] private FeverGaugeController _feverGaugeController;

        // ポーズ中かどうか
        private bool _isPaused = false;

        // ポーズ中のEnemy
        private Enemy _pausedEnemy;

        [Header("Pose Selection")]
        [SerializeField] private bool _useFakePoseForNextPose;
        [SerializeField] private PoseID _requestedFakeCommittedPoseID = PoseID.None;

        public bool IsFakePoseRequested => _useFakePoseForNextPose;
        public PoseID RequestedFakeCommittedPoseID => _requestedFakeCommittedPoseID;

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

            SetAudienceRequestSystem();

            _timer = 0f;
            CreateDebugMenu();
        }

        private void OnDestroy()
        {
            if (_poseParryJudge)
            {
                _poseParryJudge.OnParrySuccess -= OnParrySuccess;
            }

            RuntimeDebug.DebugMenu.DestroyWindow(DebugWindowName);
        }

        // 毎フレーム呼び出される
        private void Update()
        {
            if (InGameFlowLock.IsPresentationPlaying)
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
                    TryStartNextPose();
                }

                if (DebugShortcuts.Combo(Key.E, Key.P))
                {
                    TryStartNextPose();
                }
            }
        }

        // 次に開始するPoseをFakeにする
        public void RequestFakePoseForNextPose()
        {
            _useFakePoseForNextPose = true;
            _requestedFakeCommittedPoseID = PoseID.None;
        }

        // 指定Poseが確定IDになるFakePoseを次回に要求する
        public void RequestFakePoseForNextPose(PoseID committedPoseID)
        {
            if (!IsRequestablePose(committedPoseID))
            {
                Debug.LogWarning($"[EnemyPoseController] Fake pose request skipped. Reason=Unsupported committed pose. Pose={committedPoseID}");
                return;
            }

            _useFakePoseForNextPose = true;
            _requestedFakeCommittedPoseID = committedPoseID;
        }

        public void RequestFakeYaaForNextPose()
        {
            RequestFakePoseForNextPose(PoseID.Pose_Yaa);
        }

        public void RequestFakePowerForNextPose()
        {
            RequestFakePoseForNextPose(PoseID.Pose_Power);
        }

        public void RequestFakeMuscleForNextPose()
        {
            RequestFakePoseForNextPose(PoseID.Pose_Muscle);
        }

        public void ClearFakePoseRequest()
        {
            _useFakePoseForNextPose = false;
            _requestedFakeCommittedPoseID = PoseID.None;
        }

        private void CreateDebugMenu()
        {
            RuntimeDebug.DebugWindow window = RuntimeDebug.DebugMenu.CreateWindow(DebugWindowName);
            window.CreateButton("Request Fake Yaa", RequestFakeYaaForNextPose);
            window.CreateButton("Request Fake Power", RequestFakePowerForNextPose);
            window.CreateButton("Request Fake Muscle", RequestFakeMuscleForNextPose);
            window.CreateMessage("Fake Requested", () => _useFakePoseForNextPose.ToString());
            window.CreateMessage("Fake Committed Pose", () => _requestedFakeCommittedPoseID.ToString());
        }

        // ポーズ状態を解除する
        public void Unpause()
        {
            _pausedEnemy = null;
            _isPaused = false;
            if (_poseParryJudge == null) { return; }
            _poseParryJudge?.EndEnemyPose();
        }

        public void ResetForRound()
        {
            Unpause();
            ClearFakePoseRequest();
            _timer = 0f;
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

        private bool TryStartNextPose()
        {
            if (_isPaused)
            {
                return false;
            }

            SetAudienceRequestSystem();

            if (_audienceRequestSystem != null && _audienceRequestSystem.HasActiveBlackRequest)

            {

                _audienceRequestSystem.ClearBlackRequestAndRequestFakePose();
            }

            EnemyPoseType poseType = _useFakePoseForNextPose
                ? EnemyPoseType.Fake
                : EnemyPoseType.Normal;
            PoseID committedPoseID = poseType == EnemyPoseType.Fake
                ? _requestedFakeCommittedPoseID
                : PoseID.None;

            Enemy targetEnemy = _enemyRegistry?.GetRandomEnemy(poseType, committedPoseID);
            if (targetEnemy == null)
            {
                _timer = 0f;
                return false;
            }

            PoseID poseID = targetEnemy.RandomPose(poseType, committedPoseID);
            if (poseID == PoseID.None)
            {
                _timer = 0f;
                return false;
            }

            _pausedEnemy = targetEnemy;
            _isPaused = true;
            _timer = 0f;

            if (poseType == EnemyPoseType.Fake)
            {
                ClearFakePoseRequest();
            }

            return true;
        }

        private static bool IsRequestablePose(PoseID poseID)
        {
            return poseID == PoseID.Pose_Yaa ||
                   poseID == PoseID.Pose_Power ||
                   poseID == PoseID.Pose_Muscle;
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

        // AudienceRequestSystemをセットアップする
        private void SetAudienceRequestSystem()
        {
            // すでにセットアップ済みの場合は何もしない
            if (_audienceRequestSystem != null)
            {
                return;
            }

            // AudienceRequestContextがセットされていない場合は、ContextManagerから取得する
            if (_audienceRequestContext == null)
            {
                // AudienceRequestContextをContextManagerから取得する
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
            // AudienceRequestContextが取得できた場合は、AudienceRequestSystemを取得する
            _audienceRequestSystem = _audienceRequestContext?.GetAudienceRequestSystem();
        }

    }
}
