using UnityEngine;
using Context;

namespace Pose
{
    public class PoseContext : ContextBase
    {
        // 内部
        [SerializeField] private PoseParryJudge _poseParryJudge;
        public PoseParryJudge GetPoseParryJudge => _poseParryJudge;

        [SerializeField] private PoseStateTracker _stateTracker;
        public PoseStateTracker GetPoseStateTracker => _stateTracker;
    }
}
