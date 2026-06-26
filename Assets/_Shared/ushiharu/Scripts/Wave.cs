using UnityEngine;

namespace ushiharu.Sandbox
{
    public class Wave : MonoBehaviour
    {
        public GameObject enemyPrefab;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {

        }

        // Update is called once per frame
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

