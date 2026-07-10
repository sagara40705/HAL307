using System;
using UnityEngine;
using UnityEngine.Events;

namespace Enemy
{
    // Relays wave replacement presentation calls to runtime Enemy instances.
    public sealed class EnemyWavePresentationBridge : MonoBehaviour
    {
        [SerializeField] private EnemyLineupManager _enemyLineupManager;

        [Header("Default Events")]
        [SerializeField] private EnemyEvent _onRemovingEnemy;
        [SerializeField] private EnemyEvent _onIncomingEnemy;

        [Header("Named Events")]
        [SerializeField] private EnemyEventSet[] _removingEnemyEventSets;
        [SerializeField] private EnemyEventSet[] _incomingEnemyEventSets;

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

        // Invokes a named event set for the Enemy that will be removed.
        public void InvokeRemovingEnemy(string eventSetName)
        {
            InvokeEnemyEventSet(RemovingEnemy, _removingEnemyEventSets, eventSetName, "RemovingEnemy");
        }

        // Invokes events for the newly generated Enemy.
        public void InvokeIncomingEnemy()
        {
            InvokeEnemyEvent(IncomingEnemy, _onIncomingEnemy, "IncomingEnemy");
        }

        // Invokes a named event set for the newly generated Enemy.
        public void InvokeIncomingEnemy(string eventSetName)
        {
            InvokeEnemyEventSet(IncomingEnemy, _incomingEnemyEventSets, eventSetName, "IncomingEnemy");
        }

        // Invokes removing and incoming Enemy events in order.
        public void InvokeReplacementEnemies()
        {
            InvokeRemovingEnemy();
            InvokeIncomingEnemy();
        }

        // Invokes named event sets for removing and incoming Enemy instances in order.
        public void InvokeReplacementEnemies(string eventSetName)
        {
            InvokeRemovingEnemy(eventSetName);
            InvokeIncomingEnemy(eventSetName);
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

        // Empty sample for incoming Enemy presentation.
        public void SampleIncomingEnemyAction(Enemy enemy)
        {
        }

        // Empty sample for removing Enemy presentation.
        public void SampleRemovingEnemyAction(Enemy enemy)
        {
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

        private void InvokeEnemyEventSet(Enemy enemy, EnemyEventSet[] eventSets, string eventSetName, string targetName)
        {
            if (!HasPendingReplacement)
            {
                Debug.LogWarning($"[EnemyWavePresentationBridge] No pending replacement. Target={targetName}, EventSet={eventSetName}");
                _onNoPendingReplacement?.Invoke();
                return;
            }

            if (enemy == null)
            {
                Debug.LogWarning($"[EnemyWavePresentationBridge] Enemy was not found. Target={targetName}, EventSet={eventSetName}");
                return;
            }

            EnemyEventSet eventSet = FindEventSet(eventSets, eventSetName);
            if (eventSet == null)
            {
                Debug.LogWarning($"[EnemyWavePresentationBridge] Event set was not found. Target={targetName}, EventSet={eventSetName}");
                return;
            }

            eventSet.Invoke(enemy);
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

        private EnemyEventSet FindEventSet(EnemyEventSet[] eventSets, string eventSetName)
        {
            if (string.IsNullOrWhiteSpace(eventSetName) || eventSets == null || eventSets.Length <= 0)
            {
                return null;
            }

            foreach (EnemyEventSet eventSet in eventSets)
            {
                if (eventSet == null || !eventSet.IsMatch(eventSetName))
                {
                    continue;
                }

                return eventSet;
            }

            return null;
        }

        [Serializable]
        public sealed class EnemyEvent : UnityEvent<Enemy>
        {
        }

        [Serializable]
        private sealed class EnemyEventSet
        {
            [SerializeField] private string eventSetName;
            [SerializeField] private EnemyEvent onInvoke;

            public bool IsMatch(string targetName)
            {
                return string.Equals(eventSetName, targetName, StringComparison.Ordinal);
            }

            public void Invoke(Enemy enemy)
            {
                onInvoke?.Invoke(enemy);
            }
        }
    }
}
