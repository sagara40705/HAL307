using System;
using UnityEngine;

namespace Enemy
{
    // 敵枠に生成する敵データと出現位置
    [Serializable]
    public sealed class EnemyLineupEntry
    {
        [SerializeField] private EnemySlotId slot = EnemySlotId.Enemy1;
        [SerializeField] private EnemyData enemyData;
        [SerializeField] private Vector3 position;
        [SerializeField] private Vector3 rotationEuler;

        public EnemySlotId Slot => slot;
        public EnemyData EnemyData => enemyData;
        public Vector3 Position => position;
        public Quaternion Rotation => Quaternion.Euler(rotationEuler);
        public bool IsValid => enemyData != null;
    }
}
