using Pose;
using Context;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using RuntimeDebug;

namespace Player
{

    public class PlayerPoseContoroller : MonoBehaviour
    {
        [SerializeField] private PlayerContext _playerContext;
        [SerializeField] private PoseParryJudge _poseParryJudge;
        [SerializeField] private PoseStateTracker _poseStateTracker;

        [SerializeField] private Proto_PlayerColorChanger _colorChanger;
        [SerializeField] private Color _yaaColor;
        [SerializeField] private Color _powerColor;
        [SerializeField] private Color _muscleColor;


        [SerializeField] private string _currentPoseName = "None";
        [SerializeField] private Animator _animator;

        private bool _isPose = false;
        private float _poseTimer = 0f;

        public Transform MoveTargetTransform => _colorChanger != null ? _colorChanger.transform : transform;

        // ポーズ時のイベント
        public event Action<PoseID> OnPoseStart; /*あう*/

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            PoseContext poseContext = _playerContext?.GetPoseContext();
            _poseParryJudge = poseContext?.GetPoseParryJudge;
            _poseStateTracker = poseContext?.GetPoseStateTracker;
            Debug.Log("PlayerPoseController Start. _poseParryJudge = " + _poseParryJudge);
            //---animator
            if (_animator == null)
            { _animator = GetComponent<Animator>(); }

            // デバッグ用のUIを作成
            DebugWindow window = RuntimeDebug.DebugMenu.CreateWindow("Player");
            window.CreateButton("Pose Yaa", () => TryOnPose(PoseID.Pose_Yaa));
            window.CreateButton("Pose Power", () => TryOnPose(PoseID.Pose_Power));
            window.CreateButton("Pose Muscle", () => TryOnPose(PoseID.Pose_Muscle));
            window.CreateMessage("Current Pose", () => _currentPoseName);
        }

        private void OnDestroy()
        {
            if (_isPose)
            {
                _poseStateTracker?.EndPose();
            }

            RuntimeDebug.DebugMenu.DestroyWindow("Player");
        }

        // Update is called once per frame
        private void Update()
        {
            if (_isPose)
            {
                _poseTimer -= Time.deltaTime;
                if (_poseTimer <= 0f)
                {
                    StopPose();
                }
            }
            else
            {
                if (DebugShortcuts.Combo(Key.P, Key.Y))
                {
                    TryOnPose(PoseID.Pose_Yaa);
                }
                if (DebugShortcuts.Combo(Key.P, Key.W))
                {
                    TryOnPose(PoseID.Pose_Power);
                }
                if (DebugShortcuts.Combo(Key.P, Key.M))
                {
                    TryOnPose(PoseID.Pose_Muscle);
                }
            }
        }

        private void OnPose(PoseID poseID)
        {
            _isPose = true;
            _poseTimer = 3f; // ポーズ状態の持続時間を設定
            _poseStateTracker?.BeginPose();
            _poseParryJudge?.StartPlayerPose(poseID);
            ChangeColor(poseID);
            OnPoseStart?.Invoke(poseID); 
            Debug.Log($"Player started pose: {poseID}");
        }

        public void TryOnPose(PoseID poseID)
        {
            if (InGameFlowLock.IsPresentationPlaying)
            {
                return;
            }

            if (!_isPose)
            {
                OnPose(poseID);
                _currentPoseName = poseID.ToString();
            }
        }

        private void ChangeColor(PoseID poseID)
        {
            if (_animator == null)
                return;

            switch (poseID)
            {
                case PoseID.Pose_Yaa:
                    _colorChanger?.SetColor(_yaaColor);
                    _animator.SetTrigger("Yaa");
                    break;
                case PoseID.Pose_Power:
                    _colorChanger?.SetColor(_powerColor);
                    _animator.SetTrigger("Power");
                    break;
                case PoseID.Pose_Muscle:
                    _colorChanger?.SetColor(_muscleColor);
                    _animator.SetTrigger("Muscle");
                    break;
                default:
                    _colorChanger?.ResetColor();
                    break;
            }
        }

        private void StopPose()
        {
            _isPose = false;
            _poseTimer = 0f;
            _poseStateTracker?.EndPose();
            _poseParryJudge?.EndPlayerPose();
            _colorChanger?.ResetColor();
            if (_animator != null)
            {
                _animator.SetTrigger("Idle");
            }
            _currentPoseName = "None";
            Debug.Log("Player pose ended.");
        }
    }
}
