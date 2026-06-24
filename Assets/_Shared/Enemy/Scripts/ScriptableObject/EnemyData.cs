using Pose;
using UnityEngine;

namespace Enemy
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "Game/Enemy/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("Prefab")]
        [SerializeField] private GameObject enemyPrefab;

        [Header("Status")]
        [SerializeField] private int maxHp = 100;

        [Header("Pose")]
        [SerializeField] private EnemyPoseSet poseSet;

        public GameObject EnemyPrefab => enemyPrefab;
        public int MaxHp => maxHp;
        public EnemyPoseSet PoseSet => poseSet;
    }
}
