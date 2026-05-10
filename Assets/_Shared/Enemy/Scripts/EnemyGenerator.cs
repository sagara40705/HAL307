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

        // EnemyのPrefab
        [SerializeField] private Enemy _enemyPrefab;

        // Enemyを生成する場所(親)
        [SerializeField] private Transform _parent;

        public void GenerateEnemy()
        {
            if (_enemyPrefab != null)
            {
                Enemy enemy = Instantiate(_enemyPrefab, transform.position, _parent.rotation, _parent);
                Debug.Log($"Generated enemy: {enemy.name}");
                enemy.Initialize(_enemyRegistry, _poseController);
            }
        }
    }
}
