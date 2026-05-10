using System.Collections.Generic;
using UnityEngine;

namespace Pose
{
    [CreateAssetMenu(fileName = "EnemyPoseSet", menuName = "Game/Enemy/Pose Set")]
    public class EnemyPoseSet : ScriptableObject
    {
        [SerializeField] private List<EnemyPoseData> _poses = new();
        public IReadOnlyList<EnemyPoseData> Poses => _poses;

        // Setの中からランダムにPoseを1つ取得する
        public EnemyPoseData GetRandomPose()
        {
            if (_poses == null || _poses.Count == 0) { return null; }

            int index = Random.Range(0, _poses.Count);
            return _poses[index];
        }

        // Setの中に特定のPoseIDが含まれているかどうか
        public bool Contains(PoseID poseID)
        {
            foreach (EnemyPoseData pose in _poses)
            {
                if (pose == null) { continue; }

                if (pose.ID == poseID) { return true; }
            }

            return false;
        }

        // Setの中から特定のPoseIDに対応するPoseを取得する
        public EnemyPoseData GetPose(PoseID poseID)
        {
            foreach (EnemyPoseData pose in _poses)
            {
                if (pose == null) { continue; }

                if (pose.ID == poseID) { return pose; }
            }

            return null;
        }
    }
}
