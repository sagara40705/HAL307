using UnityEngine;

namespace Enemy
{
    // 2体の敵枠とWave間の入れ替えを管理
    public sealed class EnemyLineupManager : MonoBehaviour
    {
        [SerializeField] private EnemyGenerator _enemyGenerator;
        [SerializeField] private EnemyRegistry _enemyRegistry;
        [SerializeField] private global::ScoreManager _scoreManager;
        [SerializeField] private global::ForwardProgressMover _forwardProgressMover;

        [Header("Current Enemies")]
        [SerializeField] private Enemy _enemy1;
        [SerializeField] private Enemy _enemy2;

        [Header("Spawn Points")]
        [SerializeField] private Transform _enemy1SpawnPoint;
        [SerializeField] private Transform _enemy2SpawnPoint;

        [Header("Replace")]
        [SerializeField] private EnemyData _replacementEnemyData;
        [SerializeField] private bool _resetScoreOnReplace = true;

        public Enemy Enemy1 => GetEnemy(EnemySlotId.Enemy1);
        public Enemy Enemy2 => GetEnemy(EnemySlotId.Enemy2);

        private void Awake()
        {
            ResolveReferences();
            ResolveSlotEnemies();
        }

        // StageDataの初期敵設定で敵枠を初期化
        public void SetupInitialEnemies(EnemyLineupEntry[] entries)
        {
            if (entries == null)
            {
                return;
            }

            foreach (EnemyLineupEntry entry in entries)
            {
                if (entry == null || !entry.IsValid)
                {
                    continue;
                }

                ReplaceEnemy(entry.Slot, entry.EnemyData, entry.Position, entry.Rotation);
            }
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

            return _scoreManager.GetLowestEnemySlot();
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

        // 指定した枠の敵を入れ替え
        public Enemy ReplaceEnemy(EnemySlotId slot, EnemyData enemyData)
        {
            ResolveReferences();
            ResolveSlotEnemies();

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

        // EnemyRegistryからEnemy1/Enemy2を復元
        private void ResolveSlotEnemies()
        {
            GetEnemy(EnemySlotId.Enemy1);
            GetEnemy(EnemySlotId.Enemy2);
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
            return slot == EnemySlotId.Enemy1 ? _enemy1 : _enemy2;
        }

        private void SetEnemy(EnemySlotId slot, Enemy enemy)
        {
            if (slot == EnemySlotId.Enemy1)
            {
                _enemy1 = enemy;
                return;
            }

            _enemy2 = enemy;
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
            return slot == EnemySlotId.Enemy1 ? _enemy1SpawnPoint : _enemy2SpawnPoint;
        }
    }
}
