using UnityEngine;
using Pose;
using Context;
using AudioRequest;

namespace Player
{
    public class PlayerContext : ContextBase
    {
        // 内部
        [SerializeField] private PlayerPoseContoroller _poseController;
        public PlayerPoseContoroller GetPlayerPoseContoroller => _poseController;

        // Pose
        [SerializeField] private string _poseParryJudgeScene;   // PoseParryJudgeシーンの名前
        [SerializeField] private string _poseContextName; // PoseParryJudgeオブジェクトの名前
        [SerializeField] private PoseContext _poseContext;// PoseContext
        public readonly PlayerPoseContoroller GetPlayerPoseController;

        public PoseContext GetPoseContext()
        {
            if (OnlyMyScene) { return null; }
            if (_poseContext == null) { _poseContext = FindContext<PoseContext>(_poseParryJudgeScene, _poseContextName); }
            return _poseContext;
        }
    }
}
