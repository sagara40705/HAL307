using System;
using System.Collections.Generic;
using UnityEngine;

namespace Enemy
{
    [Serializable]
    public class EnemySpawnEntry
    {
        [SerializeField] private EnemyData enemyData;
        [SerializeField] private Vector3 position;
        [SerializeField] private Vector3 rotationEuler;

        public EnemyData EnemyData => enemyData;
        public Vector3 Position => position;
        public Quaternion Rotation => Quaternion.Euler(rotationEuler);
    }



    [CreateAssetMenu(fileName = "EnemySpawnTable", menuName = "Game/Enemy/Enemy Spawn Table")]
    public class EnemySpawnTable : ScriptableObject
    {
        [SerializeField] private List<EnemySpawnEntry> spawnEntries = new();

        public IReadOnlyList<EnemySpawnEntry> SpawnEntries => spawnEntries;
    }
}
