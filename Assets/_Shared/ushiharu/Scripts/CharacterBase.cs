using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;

namespace ushiharu.Sandbox
{
    public class CharacterBase : MonoBehaviour
    {
        // MonoBehaviourの生成後、最初のUpdateより前に1回だけ呼び出される
        public float speed = 3f;
        public float moveTimeMin = 2f;
        public float moveTimeMax = 5f;
        public float stopTimeMin = 1f;
        public float stopTimeMax = 3f;
        protected Renderer rend;
        public Vector3 pos;
        public Vector3 dir;
        public float timer = 0f;
        public float movetime = 5f;
        public GameObject enemyPrefab;
        Rigidbody rb;
        protected void Move()
        {

            while (timer <= movetime)
            {
                timer += Time.deltaTime;
                Debug.Log("Play!");
                return;
            }
        }
        private void Start()
        {
            rb = GetComponent<Rigidbody>();
        }

        protected void ChangeColor(Color color)
        {
            rend.material.color = color;
        }
        private void OnTriggerEnter(Collider collision)
        {
            // if (collision.tag == "Spawn")
            // {
            //     GameObject enemy = Instantiate(
            //  enemyPrefab,
            //  transform.position, // このGameObjectの位置
            //Quaternion.identity);

            //     enemy.tag = "EnemyAi2";
            // }
            if (collision.gameObject.tag == "Finish")
            {
                transform.position = GameObject.FindGameObjectWithTag(
                "Respawn").transform.position;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}
