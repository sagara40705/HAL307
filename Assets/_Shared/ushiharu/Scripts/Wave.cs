using UnityEngine;

namespace ushiharu.Sandbox
{
    public class Wave : MonoBehaviour
    {
        public GameObject enemyPrefab;

        // MonoBehaviourの生成後、最初のUpdateより前に1回だけ呼び出される
        void Start()
        {

        }

        // 毎フレーム呼び出される
        void Update()
        {
        }
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.tag == "EnemyAi")
            {
                Destroy(collision.gameObject);

                //SpawnEnemy();

            }
            if(gameObject==null)
            {
                GameObject enemy = Instantiate(
           enemyPrefab,
           transform.position, // このGameObjectの位置
           Quaternion.identity);
            }

        }


        //void SpawnEnemy()
        //{
        //    GameObject enemy = Instantiate(
        //     enemyPrefab,
        //     transform.position, // このGameObjectの位置
        //   transform.rotation);

        //    enemy.tag = "EnemyAi2";
        //}
       
    }
}

