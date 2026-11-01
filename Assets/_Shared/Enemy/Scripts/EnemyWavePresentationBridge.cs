using System;
using UnityEngine;
using UnityEngine.Events;

namespace Enemy
{
    // Wave交代演出の呼び出しを、実行時のEnemyインスタンスへ中継する。
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

        // 退場するEnemyのイベントを呼び出す。
        public void InvokeRemovingEnemy()
        {
            InvokeEnemyEvent(RemovingEnemy, _onRemovingEnemy, "RemovingEnemy");
        }

        // 退場するEnemyの、指定名のイベントセットを呼び出す。
        public void InvokeRemovingEnemy(string eventSetName)
        {
            InvokeEnemyEventSet(RemovingEnemy, _removingEnemyEventSets, eventSetName, "RemovingEnemy");
        }

        // 新しく生成されたEnemyのイベントを呼び出す。
        public void InvokeIncomingEnemy()
        {
            InvokeEnemyEvent(IncomingEnemy, _onIncomingEnemy, "IncomingEnemy");
        }

        // 新しく生成されたEnemyの、指定名のイベントセットを呼び出す。
        public void InvokeIncomingEnemy(string eventSetName)
        {
            InvokeEnemyEventSet(IncomingEnemy, _incomingEnemyEventSets, eventSetName, "IncomingEnemy");
        }

        // 退場するEnemy、新しく参加するEnemyの順にイベントを呼び出す。
        public void InvokeReplacementEnemies()
        {
            InvokeRemovingEnemy();
            InvokeIncomingEnemy();
        }

        // 退場するEnemy、新しく参加するEnemyの順に、指定名のイベントセットを呼び出す。
        public void InvokeReplacementEnemies(string eventSetName)
        {
            InvokeRemovingEnemy(eventSetName);
            InvokeIncomingEnemy(eventSetName);
        }

        // 退場するEnemyの有効状態を変更する。
        public void SetRemovingEnemyActive(bool isActive)
        {
            SetEnemyActive(RemovingEnemy, isActive, "RemovingEnemy");
        }

        // 新しく生成されたEnemyの有効状態を変更する。
        public void SetIncomingEnemyActive(bool isActive)
        {
            SetEnemyActive(IncomingEnemy, isActive, "IncomingEnemy");
        }

        // 参加するEnemyの演出用の空のサンプル。
        public void SampleIncomingEnemyAction(Enemy enemy)
        {
        }

        // 退場するEnemyの演出用の空のサンプル。
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
