using UnityEngine;
using Pose;

namespace Enemy
{
    [CreateAssetMenu(fileName = "EnemyPoseData", menuName = "Game/Enemy/Enemy Pose Data")]
    public class EnemyPoseData : ScriptableObject
    {
        [SerializeField] private PoseID _poseID;

        [Header("Basic")]
        [SerializeField, Min(0.01f)] private float _duration = 1.0f; // Poseの持続時間

        [Header("Action")]
        [SerializeField] private EnemyPoseAction _poseAction;

        public PoseID ID => _poseID;
        public float Duration => _duration;
        public EnemyPoseAction PoseAction => _poseAction;
    }
}
