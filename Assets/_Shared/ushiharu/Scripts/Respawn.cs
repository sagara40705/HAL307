using UnityEngine;

namespace ushiharu.Sandbox
{
    public class Respawn : MonoBehaviour
    {
        public GameObject enemyPrefab;
        public float Spawntime;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            //指定した時間分呼び出しを遅らせる
            Invoke("Spawn", Spawntime);
        }

        // Update is called once per frame
        void Update()
        {
        
        }
        void Spawn()
        {
            //セットしたObjectをスポーンさせる
            GameObject enemy = Instantiate(
                  enemyPrefab,
                   transform.position, // このGameObjectの位置
                   transform.rotation);

            enemy.tag = "EnemyAi";
        }
    }
}
