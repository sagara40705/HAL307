using Pose;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerPoseContoroller : MonoBehaviour
    {
        [SerializeField] private PlayerContext _playerContext;
        [SerializeField] private PoseParryJudge _poseParryJudge;

        private bool _isPose = false;
        private float _poseTimer = 0f;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _poseParryJudge = _playerContext.PoseParryJudge;
        }

        // Update is called once per frame
        void Update()
        {
            if (_isPose)
            {
                _poseTimer -= Time.deltaTime;
                if (_poseTimer <= 0f)
                {
                    _isPose = false;
                    _poseTimer = 0f;
                    _poseParryJudge.EndPlayerPose();
                    Debug.Log("Player pose ended.");
                }
            }
            else
            {
                if (DebugShortcuts.Combo(Key.P, Key.Y))
                {
                    OnPose(PoseID.Pose_Yaa);
                }
                if (DebugShortcuts.Combo(Key.P, Key.W))
                {
                    OnPose(PoseID.Pose_Power);
                }
                if (DebugShortcuts.Combo(Key.P, Key.M))
                {
                    OnPose(PoseID.Pose_Muscle);
                }
            }
        }

        private void OnPose(PoseID poseID)
        {
            _isPose = true;
            _poseTimer = 3f; // ポーズ状態の持続時間を設定
            _poseParryJudge.StartPlayerPose(poseID);
            Debug.Log($"Player started pose: {poseID}");
        }

        public void TryOnPose(PoseID poseID)
        {
            if (!_isPose)
            {
                OnPose(poseID);
            }
        }
    }
}
