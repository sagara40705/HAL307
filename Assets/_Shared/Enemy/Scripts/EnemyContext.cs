using UnityEngine;

namespace Enemy
{
    // 外部からEnemyにアクセスするためのクラス
    public class EnemyContext : MonoBehaviour
    {
        [SerializeField] private EnemyManager _enemyManager;
    }
}
