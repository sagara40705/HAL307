using UnityEngine;

namespace Enemy
{
    public class EnemyManager : MonoBehaviour
    {
        [SerializeField] private EnemyGenerator _enemyGenerator;


        private void Start()
        {
            if (_enemyGenerator == null)
            {
                Debug.LogWarning("[EnemyManager] EnemyGenerator is not assigned.");
                return;
            }

            _enemyGenerator.GenerateFromData(0);
            _enemyGenerator.GenerateFromData(1);
        }
    }
}
