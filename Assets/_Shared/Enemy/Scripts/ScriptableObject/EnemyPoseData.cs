using Pose;
using UnityEngine;
using UnityEngine.Serialization;

namespace Enemy
{
    [CreateAssetMenu(fileName = "EnemyPoseData", menuName = "Game/Enemy/Enemy Pose Data")]
    public class EnemyPoseData : ScriptableObject
    {
        [Header("Pose")]
        [SerializeField, FormerlySerializedAs("_poseID")] private PoseID _preCommitPoseID;
        [SerializeField] private PoseID _committedPoseID = PoseID.None;

        [Header("Timing")]
        [SerializeField, FormerlySerializedAs("_duration"), Min(0.01f)] private float _duration = 1.0f;
        [SerializeField, Min(0f)] private float _commitDelay = 0f;

        [Header("Action")]
        [SerializeField] private EnemyPoseAction _poseAction;

        public PoseID ID => CommittedPoseID;
        public PoseID PreCommitPoseID => _preCommitPoseID;
        public PoseID CommittedPoseID => _committedPoseID == PoseID.None ? _preCommitPoseID : _committedPoseID;
        public float Duration => _duration;
        public float CommitDelay => Mathf.Min(_commitDelay, _duration);
        public EnemyPoseAction PoseAction => _poseAction;
    }
}
