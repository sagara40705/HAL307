using System.Collections.Generic;
using UnityEngine;
using Pose;

namespace Enemy
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

        // 指定種類のPoseからランダムに1つ取得する
        public EnemyPoseData GetRandomPose(EnemyPoseType poseType)
        {
            int poseCount = GetPoseCount(poseType);
            if (poseCount == 0) { return null; }

            int targetIndex = Random.Range(0, poseCount);
            foreach (EnemyPoseData pose in _poses)
            {
                if (pose == null || pose.PoseType != poseType) { continue; }
                if (targetIndex == 0) { return pose; }

                targetIndex--;
            }

            return null;
        }

        public bool ContainsPoseType(EnemyPoseType poseType)
        {
            return GetPoseCount(poseType) > 0;
        }

        private int GetPoseCount(EnemyPoseType poseType)
        {
            if (_poses == null) { return 0; }

            int count = 0;
            foreach (EnemyPoseData pose in _poses)
            {
                if (pose != null && pose.PoseType == poseType)
                {
                    count++;
                }
            }

            return count;
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
