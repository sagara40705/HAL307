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
        private const float DefaultPoseDuration = 7f;

        private static readonly int IdleStateHash = Animator.StringToHash("Idle");
        private static readonly Color DefaultYaaColor = new Color(0f, 0.41078663f, 1f, 0f);
        private static readonly Color DefaultPowerColor = new Color(1f, 0f, 0f, 0f);
        private static readonly Color DefaultMuscleColor = new Color(0.9113277f, 1f, 0f, 0f);
        private static readonly int[] AnimationTriggerHashes =
        {
            Animator.StringToHash("Idle"),
            Animator.StringToHash("Yaa"),
            Animator.StringToHash("Power"),
            Animator.StringToHash("Muscle")
        };

        [SerializeField] private PlayerContext _playerContext;
        [SerializeField] private PoseParryJudge _poseParryJudge;
        [SerializeField] private PoseStateTracker _poseStateTracker;
        [SerializeField] private PlayerPoseSettings _poseSettings;

        [SerializeField] private Proto_PlayerColorChanger _colorChanger;


        [SerializeField] private string _currentPoseName = "None";
        [SerializeField] private Animator _animator;

        private bool _isPose = false;
        private float _poseTimer = 0f;

        public bool IsPosing => _isPose;
        public Transform MoveTargetTransform => _colorChanger != null ? _colorChanger.transform : transform;

        // ポーズ時のイベント
        public event Action<PoseID> OnPoseStart; /*あう*/

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            if (_poseSettings == null)
            {
                Debug.LogWarning("[PlayerPoseContoroller] PlayerPoseSettings is not assigned. Default pose values are used.", this);
            }

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
            _poseTimer = PoseDuration;
            _poseStateTracker?.BeginPose();
            _poseParryJudge?.StartPlayerPose(poseID);
            ChangeColor(poseID);
            OnPoseStart?.Invoke(poseID); 
            Debug.Log($"Player started pose: {poseID}");
        }

        public void TryOnPose(PoseID poseID)
        {
            Debug.Log(
                $"[PoseTrace] TryOnPose entered. " +
                $"pose={poseID}, " +
                $"IsPresentationPlaying={InGameFlowLock.IsPresentationPlaying}, " +
                $"isPose={_isPose}"
            );

            if (InGameFlowLock.IsPresentationPlaying)
            {
                Debug.Log(
                    "[PoseTrace] TryOnPose return. " +
                    "reason=InGameFlowLock.IsPresentationPlaying"
                );
                return;
            }

            if (!_isPose)
            {
                Debug.Log($"[PoseTrace] OnPose before. pose={poseID}");

                OnPose(poseID);
                _currentPoseName = poseID.ToString();

                Debug.Log($"[PoseTrace] OnPose after. pose={poseID}");
            }
            else
            {
                Debug.Log(
                    "[PoseTrace] TryOnPose return. reason=already posing"
                );
            }
        }

        private void ChangeColor(PoseID poseID)
        {
            if (_animator == null)
                return;

            ResetAnimationTriggers();

            switch (poseID)
            {
                case PoseID.Pose_Yaa:
                    _colorChanger?.SetColor(YaaColor);
                    _animator.SetTrigger("Yaa");
                    break;
                case PoseID.Pose_Power:
                    _colorChanger?.SetColor(PowerColor);
                    _animator.SetTrigger("Power");
                    break;
                case PoseID.Pose_Muscle:
                    _colorChanger?.SetColor(MuscleColor);
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
                ResetAnimationTriggers();
                _animator.Play(IdleStateHash, 0, 0f);
            }
            _currentPoseName = "None";
            Debug.Log("Player pose ended.");
        }

        private void ResetAnimationTriggers()
        {
            foreach (int triggerHash in AnimationTriggerHashes)
            {
                _animator.ResetTrigger(triggerHash);
            }
        }

        private float PoseDuration => _poseSettings != null
            ? _poseSettings.PoseDuration
            : DefaultPoseDuration;

        private Color YaaColor => _poseSettings != null
            ? _poseSettings.YaaColor
            : DefaultYaaColor;

        private Color PowerColor => _poseSettings != null
            ? _poseSettings.PowerColor
            : DefaultPowerColor;

        private Color MuscleColor => _poseSettings != null
            ? _poseSettings.MuscleColor
            : DefaultMuscleColor;
    }
}
