using UnityEngine;

namespace Enemy
{
    // 最大4体の敵枠とWave間の追加・入れ替えを管理
    public sealed class EnemyLineupManager : MonoBehaviour
    {
        [SerializeField] private EnemyGenerator _enemyGenerator;
        [SerializeField] private EnemyRegistry _enemyRegistry;
        [SerializeField] private global::ScoreManager _scoreManager;
        [SerializeField] private global::ForwardProgressMover _forwardProgressMover;

        [Header("Current Enemies")]
        [SerializeField] private Enemy _enemy1;
        [SerializeField] private Enemy _enemy2;
        [SerializeField] private Enemy _enemy3;
        [SerializeField] private Enemy _enemy4;

        [Header("Spawn Points")]
        [SerializeField] private Transform _enemy1SpawnPoint;
        [SerializeField] private Transform _enemy2SpawnPoint;
        [SerializeField] private Transform _enemy3SpawnPoint;
        [SerializeField] private Transform _enemy4SpawnPoint;

        [Header("Replace")]
        [SerializeField] private EnemyData _replacementEnemyData;
        [SerializeField] private bool _resetScoreOnReplace = true;

        private bool _hasPendingReplacement;
        private EnemySlotId _pendingReplacementSlot;
        private Enemy _pendingOldEnemy;
        private Enemy _pendingNewEnemy;
        private EnemyMaxCount _maxEnemyCount = EnemyMaxCount.Two;

        public Enemy Enemy1 => GetEnemy(EnemySlotId.Enemy1);
        public Enemy Enemy2 => GetEnemy(EnemySlotId.Enemy2);
        public Enemy Enemy3 => GetEnemy(EnemySlotId.Enemy3);
        public Enemy Enemy4 => GetEnemy(EnemySlotId.Enemy4);
        public int MaxEnemyCount => (int)_maxEnemyCount;
        public int CurrentEnemyCount => CountCurrentEnemies();
        public bool HasPendingReplacement => _hasPendingReplacement;
        public EnemySlotId PendingReplacementSlot => _pendingReplacementSlot;
        public Enemy PendingOldEnemy => _pendingOldEnemy;
        public Enemy PendingNewEnemy => _pendingNewEnemy;

        private void Awake()
        {
            ResolveReferences();
            ResolveSlotEnemies();
        }

        // StageDataの初期敵設定で敵枠を初期化
        public void SetupInitialEnemies(EnemyLineupEntry[] entries)
        {
            SetupInitialEnemies(entries, EnemyMaxCount.Two, _maxEnemyCount);
        }

        public void SetupInitialEnemies(EnemyLineupEntry[] entries, EnemyMaxCount maxEnemyCount)
        {
            SetupInitialEnemies(entries, EnemyMaxCount.Two, maxEnemyCount);
        }

        public void SetupInitialEnemies(
            EnemyLineupEntry[] entries,
            EnemyMaxCount initialEnemyCount,
            EnemyMaxCount maxEnemyCount)
        {
            ConfigureMaxEnemyCount(maxEnemyCount);
            int requiredInitialEnemyCount = Mathf.Min((int)initialEnemyCount, MaxEnemyCount);

            if (entries == null)
            {
                Debug.LogWarning($"[EnemyLineupManager] Initial enemy setup failed. Reason=Entries are null. RequiredCount={requiredInitialEnemyCount}");
                return;
            }

            foreach (EnemyLineupEntry entry in entries)
            {
                if (entry == null || !entry.IsValid)
                {
                    continue;
                }

                if (!IsInitialEnemySlot(entry.Slot, requiredInitialEnemyCount))
                {
                    continue;
                }

                ReplaceEnemy(entry.Slot, entry.EnemyData, entry.Position, entry.Rotation);
            }

            int actualInitialEnemyCount = 0;
            for (int slotNumber = 1; slotNumber <= requiredInitialEnemyCount; slotNumber++)
            {
                if (GetEnemy((EnemySlotId)slotNumber) != null)
                {
                    actualInitialEnemyCount++;
                }
            }

            if (actualInitialEnemyCount != requiredInitialEnemyCount)
            {
                Debug.LogWarning($"[EnemyLineupManager] Initial enemy count is invalid. Count={actualInitialEnemyCount}, RequiredCount={requiredInitialEnemyCount}");
            }
        }

        public void ConfigureMaxEnemyCount(EnemyMaxCount maxEnemyCount)
        {
            _maxEnemyCount = maxEnemyCount == EnemyMaxCount.Four
                ? EnemyMaxCount.Four
                : EnemyMaxCount.Two;
        }

        // 現在存在する全EnemyへWave共通のアニメーション速度を設定
        public void SetAllEnemyAnimationSpeed(float animationSpeed)
        {
            ResolveReferences();
            if (_enemyRegistry == null)
            {
                Debug.LogWarning("[EnemyLineupManager] Animation speed was not applied. Reason=EnemyRegistry is null.");
                return;
            }

            foreach (Enemy enemy in _enemyRegistry.Enemies)
            {
                enemy?.SetAnimationSpeed(animationSpeed);
            }
        }

        // 確定済みのEnemy枠数を取得
        private int CountCurrentEnemies()
        {
            int enemyCount = 0;
            for (int slotNumber = 1; slotNumber <= MaxEnemyCount; slotNumber++)
            {
                if (GetEnemy((EnemySlotId)slotNumber) != null)
                {
                    enemyCount++;
                }
            }

            return enemyCount;
        }

        // スコアが低い敵枠を取得
        public EnemySlotId GetLowestScoreSlot()
        {
            ResolveReferences();

            if (_scoreManager == null)
            {
                Debug.LogWarning("[EnemyLineupManager] ScoreManager was not found. Enemy1 is selected.");
                return EnemySlotId.Enemy1;
            }

            EnemySlotId lowestSlot = EnemySlotId.Enemy1;
            int lowestScore = int.MaxValue;
            bool foundEnemy = false;

            for (int slotNumber = 1; slotNumber <= MaxEnemyCount; slotNumber++)
            {
                EnemySlotId slot = (EnemySlotId)slotNumber;
                if (GetEnemy(slot) == null)
                {
                    continue;
                }

                int score = _scoreManager.GetEnemyScore(slot);
                if (foundEnemy && score >= lowestScore)
                {
                    continue;
                }

                lowestSlot = slot;
                lowestScore = score;
                foundEnemy = true;
            }

            if (!foundEnemy)
            {
                Debug.LogWarning("[EnemyLineupManager] Active enemy was not found. Enemy1 is selected.");
            }

            return lowestSlot;
        }

        // Inspector指定の敵データで低スコア側を入れ替え
        public Enemy ReplaceLowestScoreEnemy()
        {
            return ReplaceLowestScoreEnemy(_replacementEnemyData);
        }

        // 指定した敵データで低スコア側を入れ替え
        public Enemy ReplaceLowestScoreEnemy(EnemyData enemyData)
        {
            EnemySlotId slot = GetLowestScoreSlot();
            return ReplaceEnemy(slot, enemyData);
        }

        // 指定した敵データと出現位置で低スコア側を入れ替え
        public Enemy ReplaceLowestScoreEnemy(EnemyData enemyData, Vector3 position, Quaternion rotation)
        {
            EnemySlotId slot = GetLowestScoreSlot();
            return ReplaceEnemy(slot, enemyData, position, rotation);
        }

        public Enemy BeginLowestScoreEnemyReplacement(EnemyData enemyData, Vector3 position, Quaternion rotation)
        {
            EnemySlotId slot = GetLowestScoreSlot();
            return BeginEnemyReplacement(slot, enemyData, position, rotation);
        }

        public Enemy BeginWaveEnemyChange(EnemyData enemyData, Vector3 position, Quaternion rotation)
        {
            if (TryGetFirstAvailableSlot(out EnemySlotId availableSlot))
            {
                Debug.Log($"[EnemyLineupManager] Empty slot selected for wave enemy. Slot={availableSlot}, MaxEnemyCount={MaxEnemyCount}");
                return BeginEnemyReplacement(availableSlot, enemyData, position, rotation);
            }

            return BeginLowestScoreEnemyReplacement(enemyData, position, rotation);
        }

        public Enemy BeginEnemyReplacement(EnemySlotId slot, EnemyData enemyData, Vector3 position, Quaternion rotation)
        {
            ResolveReferences();
            ResolveSlotEnemies();

            if (_hasPendingReplacement)
            {
                Debug.LogWarning("[EnemyLineupManager] BeginEnemyReplacement skipped. Reason=Pending replacement already exists.");
                return _pendingNewEnemy;
            }

            if (!IsSlotWithinCapacity(slot))
            {
                Debug.LogWarning($"[EnemyLineupManager] BeginEnemyReplacement skipped. Reason=Slot exceeds capacity. Slot={slot}, MaxEnemyCount={MaxEnemyCount}");
                return null;
            }

            if (enemyData == null)
            {
                Debug.LogWarning("[EnemyLineupManager] BeginEnemyReplacement skipped. Reason=EnemyData is null.");
                return null;
            }

            if (_enemyGenerator == null)
            {
                Debug.LogWarning("[EnemyLineupManager] BeginEnemyReplacement skipped. Reason=EnemyGenerator is null.");
                return null;
            }

            Enemy oldEnemy = GetEnemy(slot);
            Enemy newEnemy = _enemyGenerator.Generate(enemyData, slot, position, rotation);
            if (newEnemy == null)
            {
                Debug.LogWarning($"[EnemyLineupManager] BeginEnemyReplacement failed. Slot={slot}");
                return null;
            }

            newEnemy.name = $"{slot}_Incoming";

            _hasPendingReplacement = true;
            _pendingReplacementSlot = slot;
            _pendingOldEnemy = oldEnemy;
            _pendingNewEnemy = newEnemy;

            Debug.Log($"[EnemyLineupManager] Began enemy replacement. Slot={slot}, Enemy={newEnemy.name}");
            return newEnemy;
        }

        public Enemy CommitPendingReplacement()
        {
            if (!_hasPendingReplacement)
            {
                return null;
            }

            EnemySlotId slot = _pendingReplacementSlot;
            Enemy oldEnemy = _pendingOldEnemy;
            Enemy newEnemy = _pendingNewEnemy;

            if (oldEnemy != null)
            {
                _forwardProgressMover?.UnregisterMoveTarget(oldEnemy.transform);
                Destroy(oldEnemy.gameObject);
            }

            SetEnemy(slot, null);

            if (newEnemy != null)
            {
                newEnemy.name = slot.ToString();
                SetEnemy(slot, newEnemy);
                _forwardProgressMover?.RegisterMoveTarget(newEnemy.transform);
            }

            if (_resetScoreOnReplace)
            {
                _scoreManager?.ResetEnemyScore(slot);
            }

            ClearPendingReplacement();
            Debug.Log($"[EnemyLineupManager] Committed enemy replacement. Slot={slot}, Enemy={(newEnemy != null ? newEnemy.name : "null")}");
            return newEnemy;
        }

        public void CancelPendingReplacement()
        {
            if (!_hasPendingReplacement)
            {
                return;
            }

            if (_pendingNewEnemy != null)
            {
                Destroy(_pendingNewEnemy.gameObject);
            }

            ClearPendingReplacement();
        }

        // 指定した枠の敵を入れ替え
        public Enemy ReplaceEnemy(EnemySlotId slot, EnemyData enemyData)
        {
            ResolveReferences();
            ResolveSlotEnemies();

            if (!IsSlotWithinCapacity(slot))
            {
                Debug.LogWarning($"[EnemyLineupManager] ReplaceEnemy skipped. Reason=Slot exceeds capacity. Slot={slot}, MaxEnemyCount={MaxEnemyCount}");
                return null;
            }

            if (enemyData == null)
            {
                Debug.LogWarning("[EnemyLineupManager] ReplaceEnemy skipped. Reason=EnemyData is null.");
                return null;
            }

            if (_enemyGenerator == null)
            {
                Debug.LogWarning("[EnemyLineupManager] ReplaceEnemy skipped. Reason=EnemyGenerator is null.");
                return null;
            }

            Enemy oldEnemy = GetEnemy(slot);
            Vector3 position = GetSpawnPosition(slot, oldEnemy);
            Quaternion rotation = GetSpawnRotation(slot, oldEnemy);
            return ReplaceEnemyInternal(slot, enemyData, position, rotation, oldEnemy);
        }

        // 指定した枠の敵を指定位置に入れ替え
        public Enemy ReplaceEnemy(EnemySlotId slot, EnemyData enemyData, Vector3 position, Quaternion rotation)
        {
            ResolveReferences();
            ResolveSlotEnemies();

            if (!IsSlotWithinCapacity(slot))
            {
                Debug.LogWarning($"[EnemyLineupManager] ReplaceEnemy skipped. Reason=Slot exceeds capacity. Slot={slot}, MaxEnemyCount={MaxEnemyCount}");
                return null;
            }

            if (enemyData == null)
            {
                Debug.LogWarning("[EnemyLineupManager] ReplaceEnemy skipped. Reason=EnemyData is null.");
                return null;
            }

            if (_enemyGenerator == null)
            {
                Debug.LogWarning("[EnemyLineupManager] ReplaceEnemy skipped. Reason=EnemyGenerator is null.");
                return null;
            }

            Enemy oldEnemy = GetEnemy(slot);
            return ReplaceEnemyInternal(slot, enemyData, position, rotation, oldEnemy);
        }

        // 指定した枠の現在の敵を取得
        public Enemy GetEnemy(EnemySlotId slot)
        {
            Enemy assignedEnemy = GetAssignedEnemy(slot);
            if (assignedEnemy != null)
            {
                return assignedEnemy;
            }

            ResolveReferences();
            if (_enemyRegistry == null)
            {
                return null;
            }

            foreach (Enemy enemy in _enemyRegistry.Enemies)
            {
                if (enemy == null || enemy.name != slot.ToString())
                {
                    continue;
                }

                SetEnemy(slot, enemy);
                return enemy;
            }

            return null;
        }

        // Inspector未設定時の参照解決
        private void ResolveReferences()
        {
            if (_enemyGenerator == null)
            {
                _enemyGenerator = FindFirstObjectByType<EnemyGenerator>();
            }

            if (_enemyRegistry == null)
            {
                _enemyRegistry = FindFirstObjectByType<EnemyRegistry>();
            }

            if (_scoreManager == null)
            {
                _scoreManager = global::ScoreManager.Instance != null
                    ? global::ScoreManager.Instance
                    : FindFirstObjectByType<global::ScoreManager>();
            }

            if (_forwardProgressMover == null)
            {
                _forwardProgressMover = FindFirstObjectByType<global::ForwardProgressMover>();
            }
        }

        // EnemyRegistryから現在の最大数までのEnemyを復元
        private void ResolveSlotEnemies()
        {
            for (int slotNumber = 1; slotNumber <= MaxEnemyCount; slotNumber++)
            {
                GetEnemy((EnemySlotId)slotNumber);
            }
        }

        private Enemy ReplaceEnemyInternal(EnemySlotId slot, EnemyData enemyData, Vector3 position, Quaternion rotation, Enemy oldEnemy)
        {
            if (oldEnemy != null)
            {
                _forwardProgressMover?.UnregisterMoveTarget(oldEnemy.transform);
                Destroy(oldEnemy.gameObject);
                SetEnemy(slot, null);
            }

            Enemy newEnemy = _enemyGenerator.Generate(enemyData, slot, position, rotation);
            if (newEnemy == null)
            {
                Debug.LogWarning($"[EnemyLineupManager] ReplaceEnemy failed. Slot={slot}");
                return null;
            }

            SetEnemy(slot, newEnemy);
            _forwardProgressMover?.RegisterMoveTarget(newEnemy.transform);

            if (_resetScoreOnReplace)
            {
                _scoreManager?.ResetEnemyScore(slot);
            }

            Debug.Log($"[EnemyLineupManager] Replaced enemy. Slot={slot}, Enemy={newEnemy.name}");
            return newEnemy;
        }

        private Enemy GetAssignedEnemy(EnemySlotId slot)
        {
            switch (slot)
            {
                case EnemySlotId.Enemy1:
                    return _enemy1;
                case EnemySlotId.Enemy2:
                    return _enemy2;
                case EnemySlotId.Enemy3:
                    return _enemy3;
                case EnemySlotId.Enemy4:
                    return _enemy4;
                default:
                    return null;
            }
        }

        private void SetEnemy(EnemySlotId slot, Enemy enemy)
        {
            if (slot == EnemySlotId.Enemy1)
            {
                _enemy1 = enemy;
                return;
            }

            if (slot == EnemySlotId.Enemy2)
            {
                _enemy2 = enemy;
                return;
            }

            if (slot == EnemySlotId.Enemy3)
            {
                _enemy3 = enemy;
                return;
            }

            if (slot == EnemySlotId.Enemy4)
            {
                _enemy4 = enemy;
            }
        }

        private void ClearPendingReplacement()
        {
            _hasPendingReplacement = false;
            _pendingOldEnemy = null;
            _pendingNewEnemy = null;
        }

        private Vector3 GetSpawnPosition(EnemySlotId slot, Enemy currentEnemy)
        {
            Transform spawnPoint = GetSpawnPoint(slot);
            if (spawnPoint != null)
            {
                return spawnPoint.position;
            }

            return currentEnemy != null ? currentEnemy.transform.position : Vector3.zero;
        }

        private Quaternion GetSpawnRotation(EnemySlotId slot, Enemy currentEnemy)
        {
            Transform spawnPoint = GetSpawnPoint(slot);
            if (spawnPoint != null)
            {
                return spawnPoint.rotation;
            }

            return currentEnemy != null ? currentEnemy.transform.rotation : Quaternion.identity;
        }

        private Transform GetSpawnPoint(EnemySlotId slot)
        {
            switch (slot)
            {
                case EnemySlotId.Enemy1:
                    return _enemy1SpawnPoint;
                case EnemySlotId.Enemy2:
                    return _enemy2SpawnPoint;
                case EnemySlotId.Enemy3:
                    return _enemy3SpawnPoint;
                case EnemySlotId.Enemy4:
                    return _enemy4SpawnPoint;
                default:
                    return null;
            }
        }

        private bool TryGetFirstAvailableSlot(out EnemySlotId availableSlot)
        {
            ResolveSlotEnemies();

            for (int slotNumber = 1; slotNumber <= MaxEnemyCount; slotNumber++)
            {
                EnemySlotId slot = (EnemySlotId)slotNumber;
                if (GetEnemy(slot) != null)
                {
                    continue;
                }

                availableSlot = slot;
                return true;
            }

            availableSlot = EnemySlotId.Enemy1;
            return false;
        }

        private bool IsSlotWithinCapacity(EnemySlotId slot)
        {
            int slotNumber = (int)slot;
            return slotNumber >= (int)EnemySlotId.Enemy1 && slotNumber <= MaxEnemyCount;
        }

        private static bool IsInitialEnemySlot(EnemySlotId slot, int initialEnemyCount)
        {
            int slotNumber = (int)slot;
            return slotNumber >= (int)EnemySlotId.Enemy1 && slotNumber <= initialEnemyCount;
        }
    }
}
