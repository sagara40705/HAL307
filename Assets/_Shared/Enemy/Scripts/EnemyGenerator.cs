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
            Enemy enemy = Instantiate(entry.EnemyData.EnemyPrefab, entry.Position, entry.Rotation, _parent);

            // 名前を付ける
            int enemyNumber = _enemyRegistry.Count + 1;
            enemy.name = $"Enemy{enemyNumber}";

           // Debug.Log($"Generated enemy from data: {enemy.name}");

            PoseParryJudge poseParryJudge = _enemyContext?.GetPoseContext()?.GetPoseParryJudge;
            enemy.Initialize(_enemyRegistry, _poseController, entry.EnemyData, poseParryJudge);

            if (_enemyContext != null)
            {
                enemy.OnPoseStart += (poseID) => _enemyContext.NotifyEnemyPoseStart(enemy, poseID);
            }
        }
    }
}
