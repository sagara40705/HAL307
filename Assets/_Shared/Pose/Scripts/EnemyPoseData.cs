using UnityEngine;

namespace Pose
{
    [CreateAssetMenu(fileName = "EnemyPoseData", menuName = "Game/Enemy/Enemy Pose Data")]
    public class EnemyPoseData : ScriptableObject
    {
        [SerializeField] private PoseID _poseID;

        [Header("Basic")]
        [SerializeField, Min(0.01f)] private float _duration = 1.0f; // Poseの持続時間

        public PoseID ID => _poseID;
        public float Duration => _duration;
    }
}
