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

        [SerializeField] private Proto_PlayerColorChanger _colorChanger;
        [SerializeField] private Color _yaaColor;
        [SerializeField] private Color _powerColor;
        [SerializeField] private Color _muscleColor;


        [SerializeField] private string _currentPoseName = "None";  

        private bool _isPose = false;
        private float _poseTimer = 0f;

        // ポーズ時のイベント
        public event Action<PoseID> OnPoseStart; /*あう*/

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        private void Start()
        {
            _poseParryJudge = _playerContext?.GetPoseContext()?.GetPoseParryJudge;
            Debug.Log("PlayerPoseController Start. _poseParryJudge = " + _poseParryJudge);

            // デバッグ用のUIを作成
            DebugWindow window = RuntimeDebug.DebugMenu.CreateWindow("Player");
            window.CreateButton("Pose Yaa", () => TryOnPose(PoseID.Pose_Yaa));
            window.CreateButton("Pose Power", () => TryOnPose(PoseID.Pose_Power));
            window.CreateButton("Pose Muscle", () => TryOnPose(PoseID.Pose_Muscle));
            window.CreateMessage("Current Pose", () => _currentPoseName);
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
            _poseParryJudge?.StartPlayerPose(poseID);
            ChangeColor(poseID);
            Debug.Log($"Player started pose: {poseID}");
        }

        public void TryOnPose(PoseID poseID)
        {
            if (!_isPose)
            {
                OnPose(poseID);
                _currentPoseName = poseID.ToString();
            }
        }

        private void ChangeColor(PoseID poseID)
        {
            switch (poseID)
            {
                case PoseID.Pose_Yaa:
                    _colorChanger?.SetColor(_yaaColor);
                    break;
                case PoseID.Pose_Power:
                    _colorChanger?.SetColor(_powerColor);
                    break;
                case PoseID.Pose_Muscle:
                    _colorChanger?.SetColor(_muscleColor);
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
            _poseParryJudge?.EndPlayerPose();
            _colorChanger?.ResetColor();
            _currentPoseName = "None";
            Debug.Log("Player pose ended.");
        }
    }
}
