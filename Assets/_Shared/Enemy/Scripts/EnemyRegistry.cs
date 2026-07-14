using System.Collections.Generic;
using UnityEngine;

namespace Enemy
{
    // Enemyのリスト管理するクラス
    public class EnemyRegistry : MonoBehaviour
    {
        // Enemyのリスト
        private readonly List<Enemy> _enemies = new();

        public IReadOnlyList<Enemy> Enemies => _enemies;
        public int Count => _enemies.Count;

        // Enemyをリストに登録する
        public void Register(Enemy enemy)
        {
            if (enemy == null) { return; }

            if (_enemies.Contains(enemy)) { return; }

            Debug.Log($"Registering enemy: {enemy.name}");
            _enemies.Add(enemy);
        }

        // Enemyをリストから削除する
        public void Unregister(Enemy enemy)
        {
            if (enemy == null) { return; }

            bool resulat =  _enemies.Remove(enemy);
            if (resulat) { Debug.Log($"Unregistering enemy: {enemy.name}"); }
            else { Debug.LogWarning($"Failed to unregister enemy: {enemy.name} not found in registry."); }
        }

        // Enemyを全てリストから削除する
        public void Clear()
        {
            _enemies.Clear();
            //Debug.Log("Cleared all enemies from registry.");
        }

        // ランダムにEnemyを1体取得する
        public Enemy GetRandomEnemy()
        {
            if (_enemies.Count == 0)
            {
                Debug.LogWarning("No enemies in registry to get.");
                return null;
            }
            int randomIndex = Random.Range(0, _enemies.Count);
            Enemy randomEnemy = _enemies[randomIndex];
            //Debug.Log($"Randomly selected enemy: {randomEnemy.name}");
            return randomEnemy;
        }

        // 指定種類のPoseを持つEnemyからランダムに1体取得する
        public Enemy GetRandomEnemy(EnemyPoseType poseType, Pose.PoseID committedPoseID = Pose.PoseID.None)
        {
            int enemyCount = 0;
            foreach (Enemy enemy in _enemies)
            {
                if (enemy != null && enemy.HasPoseOfType(poseType, committedPoseID))
                {
                    enemyCount++;
                }
            }

            if (enemyCount == 0)
            {
                Debug.LogWarning($"No enemies have a {poseType} pose. CommittedPose={committedPoseID}");
                return null;
            }

            int targetIndex = Random.Range(0, enemyCount);
            foreach (Enemy enemy in _enemies)
            {
                if (enemy == null || !enemy.HasPoseOfType(poseType, committedPoseID)) { continue; }
                if (targetIndex == 0) { return enemy; }

                targetIndex--;
            }

            return null;
        }
    }
}
