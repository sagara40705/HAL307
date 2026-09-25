using Pose;
using UnityEngine;

namespace Enemy
{
    // Enemyを生成するクラス
    public class EnemyGenerator : MonoBehaviour
    {
        // EnemyRegistry
        [SerializeField] private EnemyRegistry _enemyRegistry;

        // EnemyPoseController
        [SerializeField] private EnemyPoseController _poseController;

        // EnemyContext
        [SerializeField] private EnemyContext _enemyContext;

        // Enemyを生成する場所(親)
        [SerializeField] private Transform _parent;

        // スポーンデータ
        [SerializeField] private EnemySpawnTable _spawnData;


        public void GenerateFromData(int index)
        {
            if (_spawnData == null) { return; }
            if (index < 0 || index >= _spawnData.SpawnEntries.Count) { return; }

            EnemySpawnEntry entry = _spawnData.SpawnEntries[index];
            if (entry == null || _enemyRegistry == null)
            {
                Debug.LogWarning("[EnemyGenerator] GenerateFromData skipped. Entry or EnemyRegistry is missing.");
                return;
            }

            string enemyName = $"Enemy{_enemyRegistry.Count + 1}";
            GenerateInternal(entry.EnemyData, enemyName, entry.Position, entry.Rotation);
        }

        public Enemy Generate(EnemyData enemyData, EnemySlotId slot, Vector3 position, Quaternion rotation)
        {
            return GenerateInternal(enemyData, slot.ToString(), position, rotation);
        }

        private Enemy GenerateInternal(EnemyData enemyData, string enemyName, Vector3 position, Quaternion rotation)
        {
            if (enemyData == null)
            {
                Debug.LogWarning("[EnemyGenerator] Generate skipped. Reason=EnemyData is null.");
                return null;
            }

            if (enemyData.EnemyPrefab == null)
            {
                Debug.LogWarning("[EnemyGenerator] Generate skipped. Reason=EnemyPrefab is null.");
                return null;
            }

            GameObject instance = Instantiate(enemyData.EnemyPrefab, position, rotation, _parent);
            Enemy enemy = instance.GetComponent<Enemy>();
            if (enemy == null)
            {
                Debug.LogWarning("[EnemyGenerator] Generate failed. Reason=Enemy component is missing.");
                Destroy(instance);
                return null;
            }

            // EnemySlotIdと同じ名前を設定
            enemy.name = enemyName;

            PoseParryJudge poseParryJudge = _enemyContext?.GetPoseContext()?.GetPoseParryJudge;
            enemy.Initialize(_enemyRegistry, _poseController, enemyData, poseParryJudge);

            if (_enemyContext != null)
            {
                enemy.OnPoseStart += (poseID) => _enemyContext.NotifyEnemyPoseStart(enemy, poseID);
            }

            return enemy;
        }
    }
}
