using System;
using Pose;
using RuntimeDebug;
using UnityEngine;

namespace Enemy
{
    public enum EnemyPoseState
    {
        None,
        PreCommit,
        ParryGrace,
        Completed
    }

    public class Enemy : MonoBehaviour
    {
        //　EnemyData
        private EnemyData _data;

        // 管理クラスの参照
        private EnemyRegistry _enemyRegistry;
        private EnemyPoseController _poseController;
        private PoseParryJudge _poseParryJudge;

        // Pose状態を取得するContext
        [SerializeField] private string _poseContextName = "PoseContext";
        [SerializeField] private PoseStateTracker _poseStateTracker;


        //Enemyの状態
        private bool _isPaused = false;
        private PoseID _currentPoseID = PoseID.None;
        private float _poseTimer = 0f;
        private float _poseElapsedTime = 0f;
        private float _poseStateElapsedTime = 0f;
        private float _parryGraceTimer = 0f;
        private EnemyPoseData _currentPoseData;
        private EnemyPoseState _poseState = EnemyPoseState.None;
        private bool _isPoseCommitted = false;
        private bool _hasEnemyRequestNotified = false;
        private bool _isPoseTracked = false;

        // パリイ関連
        private bool _isParried = false;
        private float _parryTimer = 0f;

        //
        Proto_EnemyChangeColor _colorChanger;

        // Pose開始イベント
        public event Action<PoseID> OnPoseStart;

        public EnemyPoseState PoseState => _poseState;
        public float PoseStateElapsedTime => _poseStateElapsedTime;

        private Animator animator;
        public Animator GetAnimator => animator;

        void Start()
        {
            SetupPoseStateTracker();

            _colorChanger = GetComponent<Proto_EnemyChangeColor>();
            if (_colorChanger == null)
            {
                Debug.LogWarning($"Enemy {name} does not have a Proto_EnemyChangeColor component.");
            }

            // デバッグ用のUIを作成
            DebugWindow window = RuntimeDebug.DebugMenu.CreateWindow(name);
            window.CreateMessage("Current Pose", () => _currentPoseID.ToString());
            window.CreateMessage("Pose State", () => _poseState.ToString());
            window.CreateMessage("Pose Timer", () => _poseTimer.ToString("F2"));
            window.CreateMessage("Parry Grace", () => _parryGraceTimer.ToString("F2"));
            window.CreateMessage("Parried", () => _isParried.ToString());
            window.CreateMessage("Parry Timer", () => _parryTimer.ToString("F2"));

            animator = GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogWarning("enemy animator null");
            }
        }

        private void Update()
        {
            if (_isPaused)
            {
                float deltaTime = Time.deltaTime;

                _poseElapsedTime += deltaTime;
                _poseStateElapsedTime += deltaTime;
                _currentPoseData?.PoseAction?.OnUpdate(this, deltaTime);

                if (!_isPoseCommitted && _currentPoseData != null && _poseElapsedTime >= _currentPoseData.CommitDelay)
                {
                    _currentPoseID = _currentPoseData.CommittedPoseID;
                    CommitCurrentPose();
                    _currentPoseData.PoseAction?.OnCommitPose(this);
                }

                if (_isPoseCommitted && !_hasEnemyRequestNotified)
                {
                    _parryGraceTimer -= deltaTime;
                    if (_parryGraceTimer <= 0f)
                    {
                        CompleteParryGrace();
                    }
                }

                _poseTimer -= deltaTime;
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
                }
            }
        }

        // Enemyが生成されたときの初期化
        public void Initialize(EnemyRegistry enemyRegistry, EnemyPoseController poseController, EnemyData data, PoseParryJudge poseParryJudge)
        {
            // EnemyRegistryを受け取って登録する
            _enemyRegistry = enemyRegistry;
            _enemyRegistry?.Register(this);

            // EnemyPoseControllerを受け取る
            _poseController = poseController;

            // EnemyDataを受け取る
            _data = data;

            _poseParryJudge = poseParryJudge;
        }

        // Pose終了時の共通処理
        private void OnUnpause()
        {
            _isPaused = false;
            // ここにPose終了時の共通処理を記述する
            if (_currentPoseData) { _currentPoseData.PoseAction?.OnEnd(this); }
            EndPoseTrack();

            _poseController?.Unpause();
            _currentPoseID = PoseID.None;
            _poseTimer = 0f;
            _poseElapsedTime = 0f;
            _poseStateElapsedTime = 0f;
            _parryGraceTimer = 0f;
            _currentPoseData = null;
            SetPoseState(EnemyPoseState.None);
            _isPoseCommitted = false;
            _hasEnemyRequestNotified = false;
        }

        // Enemyが破壊されたときのクリーンアップ
        private void OnDestroy()
        {
            EndPoseTrack();

            // EnemyRegistryから登録解除する
            _enemyRegistry?.Unregister(this);
            RuntimeDebug.DebugMenu.DestroyWindow(name);
        }

        public PoseID RandomPose()
        {
            if (InGameFlowLock.IsPresentationPlaying)
            {
                return PoseID.None;
            }

            if (_data == null || _data.PoseSet.Poses.Count == 0)
            {
                return PoseID.None;
            }
            EnemyPoseData randomPoseData = _data.PoseSet.GetRandomPose();
            Pause(randomPoseData);
            return randomPoseData.ID;
        }

        public void Pause(EnemyPoseData poseData)
        {
            if (InGameFlowLock.IsPresentationPlaying)
            {
                return;
            }

            _currentPoseID = poseData.PreCommitPoseID;
            _isPaused = true;
            _poseTimer = poseData.Duration;
            _poseElapsedTime = 0f;
            _poseStateElapsedTime = 0f;
            _parryGraceTimer = 0f;
            _currentPoseData = poseData;
            SetPoseState(EnemyPoseState.PreCommit);
            _isPoseCommitted = false;
            _hasEnemyRequestNotified = false;
            BeginPoseTrack();

            //Debug.Log($"Enemy {name} paused with pose {_currentPoseID}.");
            _currentPoseData?.PoseAction?.OnStart(this);
        }

        public void CommitCurrentPose()
        {
            if (!_isPaused) { return; }
            if (_currentPoseData == null) { return; }
            if (_isPoseCommitted) { return; }

            _isPoseCommitted = true;
            SetPoseState(EnemyPoseState.ParryGrace);
            _parryGraceTimer = _currentPoseData.ParryGraceDuration;
            _poseParryJudge?.StartEnemyPose(_currentPoseID);
        }

        private void CompleteParryGrace()
        {
            if (!_isPaused) { return; }
            if (!_isPoseCommitted) { return; }
            if (_hasEnemyRequestNotified) { return; }

            _hasEnemyRequestNotified = true;
            SetPoseState(EnemyPoseState.Completed);
            _currentPoseData?.PoseAction?.OnUpdate(this, 0f);
            _poseParryJudge?.EndEnemyPose();
            OnPoseStart?.Invoke(_currentPoseID);
        }

        private void SetPoseState(EnemyPoseState poseState)
        {
            if (_poseState == poseState) { return; }

            _poseState = poseState;
            _poseStateElapsedTime = 0f;
        }

        private void SetupPoseStateTracker()
        {
            if (_poseStateTracker != null)
            {
                return;
            }

            PoseContext poseContext = null;
            ContextManager contextManager = ContextManager.Instance;
            if (contextManager != null && !string.IsNullOrWhiteSpace(_poseContextName))
            {
                poseContext = contextManager.Get<PoseContext>(_poseContextName);
            }

            if (poseContext == null)
            {
                poseContext = FindFirstObjectByType<PoseContext>();
            }

            _poseStateTracker = poseContext?.GetPoseStateTracker;
        }


        private void BeginPoseTrack()
        {
            if (_isPoseTracked)
            {
                return;
            }

            SetupPoseStateTracker();
            if (_poseStateTracker == null)
            {
                return;
            }

            _poseStateTracker.BeginPose();
            _isPoseTracked = true;
        }

        private void EndPoseTrack()
        {
            if (!_isPoseTracked)
            {
                return;
            }

            _poseStateTracker?.EndPose();
            _isPoseTracked = false;
        }

        //　パリイされたときの処理
        public void OnParrySuccess()
        {
            //Debug.Log($"Enemy {name} was parried with pose {_currentPoseID}.");
            OnUnpause();
            _colorChanger?.SetColor(Color.black);
            _isParried = true;
            _parryTimer = 3.0f;
        }
    }
}
