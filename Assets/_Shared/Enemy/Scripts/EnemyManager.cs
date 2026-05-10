using UnityEngine;

namespace Enemy
{
    public class EnemyManager : MonoBehaviour
    {
        [SerializeField] private EnemyGenerator _enemyGenerator;


        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            _enemyGenerator.GenerateEnemy();
        }

        // Update is called once per frame
        void Update()
        {
        }
    }
}
