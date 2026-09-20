using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using ushiharu.Sandbox;
using UnityEngine.InputSystem.Switch;



namespace ushiharu.Sandbox
{

    public class BaceAI : CharacterBase
    {
        public Color stopColor2 = Color.red;
        int[] Poose = new int [5];
        int randomPoose;
        public Vector3 moveDirection= Vector3.forward;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            StartCoroutine(AIBehaviour());
            rend = GetComponent<Renderer>();
        }
        void Update()
        {

        }

        // Update is called once per frame
        IEnumerator AIBehaviour()
        {
            while (true)
            {
                // 正規化（念のため）
               dir = moveDirection.normalized;
               randomPoose = Random.Range(0, Poose.Length);
                timer = 0f;
                float stoptime = Random.Range(stopTimeMin, stopTimeMax);
                float stop = 0f;
                if (Random.value <= 0.6)
                {

                    while (timer <= movetime)
                    {
                        transform.position += dir * speed * Time.deltaTime;
                        Debug.Log("P!");
                        AiState();
                        timer += Time.deltaTime * 1.5f;
                        yield return null;
                    }
                }
           yield return new WaitForSeconds(stoptime);
            }
        }

        void AiState()
        {
            switch (randomPoose)
            {
                case 1:
                    float timer2 = 0f;
                    while (timer2 <= movetime)
                    {
                        Debug.Log("!S");
                        timer2 += Time.deltaTime * 1.5f;
                    }
                    break;
                case 2:
                    Move();
                    break;
                case 3:
                    Debug.Log("M!");
                    break;
                case 4:
                    break;
            }
        }

    }
}
