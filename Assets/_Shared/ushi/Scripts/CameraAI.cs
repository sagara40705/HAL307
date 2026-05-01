using UnityEngine;
using System.Collections;

public class CameraAI : MonoBehaviour
{
    public float speed = 3f;
    public float moveTimeMin = 2f;
    public float moveTimeMax = 5f;
    public float stopTimeMin = 1f;
    public float stopTimeMax = 3f;
    // ← ここで方向を決める（例：前方向）
    public Vector3 moveDirection = Vector3.forward;
    //public Color moveColor = Color.white; // 動いてるとき
    //public Color stopColor = Color.red;   // 止まってるとき

    //  private Vector3 moveDirection;
    private Renderer rend;
    void Start()
    {
       // rend = GetComponent<Renderer>();
        StartCoroutine(AIBehaviour());
    }

    IEnumerator AIBehaviour()
    {
        while (true)
        {
            // 動く状態
           // rend.material.color = moveColor;
            // 正規化（念のため）
            Vector3 dir = moveDirection.normalized;


            // 動く時間
            float moveTime = Random.Range(moveTimeMin, moveTimeMax);
            float timer = 0f;


            while (timer < moveTime)
            {
                //決めた時間だけ、決めた方向に動く
                transform.position += moveDirection * speed * Time.deltaTime;
                timer += Time.deltaTime;
                yield return null;
            }

            // 止まる時間
            float stopTime = Random.Range(stopTimeMin, stopTimeMax);
           
            yield return new WaitForSeconds(stopTime);

        }
    }
}
