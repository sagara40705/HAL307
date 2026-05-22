using Pose;
using UnityEngine;

namespace Enemy
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "Game/Enemy/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("Prefab")]
        [SerializeField] private Enemy enemyPrefab;

        [Header("Status")]
        [SerializeField] private int maxHp = 100;

        [Header("Pose")]
        [SerializeField] private EnemyPoseSet poseSet;

        public Enemy EnemyPrefab => enemyPrefab;
        public int MaxHp => maxHp;
        public EnemyPoseSet PoseSet => poseSet;
    }
}
