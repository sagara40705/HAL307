using System;
using UnityEngine;
using UnityEngine.Events;

namespace Enemy
{
    // Relays wave replacement presentation calls to runtime Enemy instances.
    public sealed class EnemyWavePresentationBridge : MonoBehaviour
    {
        [SerializeField] private EnemyLineupManager _enemyLineupManager;
        [SerializeField] private EnemyEvent _onRemovingEnemy;
        [SerializeField] private EnemyEvent _onIncomingEnemy;
        [SerializeField] private UnityEvent _onNoPendingReplacement;

        public bool HasPendingReplacement => Lineup != null && Lineup.HasPendingReplacement;
        public EnemySlotId PendingReplacementSlot => Lineup != null ? Lineup.PendingReplacementSlot : EnemySlotId.Enemy1;
        public Enemy RemovingEnemy => Lineup != null ? Lineup.PendingOldEnemy : null;
        public Enemy IncomingEnemy => Lineup != null ? Lineup.PendingNewEnemy : null;
        public Transform RemovingEnemyTransform => RemovingEnemy != null ? RemovingEnemy.transform : null;
        public Transform IncomingEnemyTransform => IncomingEnemy != null ? IncomingEnemy.transform : null;

        private EnemyLineupManager Lineup
        {
            get
            {
                ResolveLineupManager();
                return _enemyLineupManager;
            }
        }

        private void Awake()
        {
            ResolveLineupManager();
        }

        // Invokes events for the Enemy that will be removed.
        public void InvokeRemovingEnemy()
        {
            InvokeEnemyEvent(RemovingEnemy, _onRemovingEnemy, "RemovingEnemy");
        }

        // Invokes events for the newly generated Enemy.
        public void InvokeIncomingEnemy()
        {
            InvokeEnemyEvent(IncomingEnemy, _onIncomingEnemy, "IncomingEnemy");
        }

        // Invokes removing and incoming Enemy events in order.
        public void InvokeReplacementEnemies()
        {
            InvokeRemovingEnemy();
            InvokeIncomingEnemy();
        }

        // Changes active state of the Enemy that will be removed.
        public void SetRemovingEnemyActive(bool isActive)
        {
            SetEnemyActive(RemovingEnemy, isActive, "RemovingEnemy");
        }

        // Changes active state of the newly generated Enemy.
        public void SetIncomingEnemyActive(bool isActive)
        {
            SetEnemyActive(IncomingEnemy, isActive, "IncomingEnemy");
        }

        private void InvokeEnemyEvent(Enemy enemy, EnemyEvent targetEvent, string targetName)
        {
            if (!HasPendingReplacement)
            {
                Debug.LogWarning($"[EnemyWavePresentationBridge] No pending replacement. Target={targetName}");
                _onNoPendingReplacement?.Invoke();
                return;
            }

            if (enemy == null)
            {
                Debug.LogWarning($"[EnemyWavePresentationBridge] Enemy was not found. Target={targetName}");
                return;
            }

            targetEvent?.Invoke(enemy);
        }

        private void SetEnemyActive(Enemy enemy, bool isActive, string targetName)
        {
            if (enemy == null)
            {
                Debug.LogWarning($"[EnemyWavePresentationBridge] Enemy was not found. Target={targetName}");
                return;
            }

            enemy.gameObject.SetActive(isActive);
        }

        private void ResolveLineupManager()
        {
            if (_enemyLineupManager != null)
            {
                return;
            }

            _enemyLineupManager = FindFirstObjectByType<EnemyLineupManager>();
        }

        [Serializable]
        public sealed class EnemyEvent : UnityEvent<Enemy>
        {
        }
    }
}
