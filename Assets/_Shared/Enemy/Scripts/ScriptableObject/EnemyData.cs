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

        [Header("Animation")]
        [SerializeField, Min(0f)] private float animationSpeed = 1.0f;

        [Header("Pose")]
        [SerializeField] private EnemyPoseSet poseSet;

        public GameObject EnemyPrefab => enemyPrefab;
        public int MaxHp => maxHp;
        public float AnimationSpeed => Mathf.Max(0f, animationSpeed);
        public EnemyPoseSet PoseSet => poseSet;
    }
}
