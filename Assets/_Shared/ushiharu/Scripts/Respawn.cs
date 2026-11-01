using UnityEngine;

namespace ushiharu.Sandbox
{
    public class Respawn : MonoBehaviour
    {
        public GameObject enemyPrefab;
        public float Spawntime;
        // MonoBehaviourの生成後、最初のUpdateより前に1回だけ呼び出される
        void Start()
        {
            //指定した時間分呼び出しを遅らせる
            Invoke("Spawn", Spawntime);
        }

        // 毎フレーム呼び出される
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
