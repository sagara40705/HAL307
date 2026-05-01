using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;

public class SimpleAI : MonoBehaviour
{
    public float speed = 3f;
    public float moveTimeMin = 2f;
    public float moveTimeMax = 5f;
    public float stopTimeMin = 1f;
    public float stopTimeMax = 3f;

    public Color moveColor = Color.white; // 動いてるとき
    public Color stopColor = Color.red;   // 止まってるとき
    public Color ParryColor = Color.blue;  // パリィ状態のとき
    // ← ここで方向を決める（例：前方向）
    public Vector3 moveDirection = Vector3.forward;
    private Renderer rend;
    void Start()
    {
        rend = GetComponent<Renderer>();
        StartCoroutine(AIBehaviour());
    }

    void Push()
    {
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            Debug.Log("Push!");
            // 止まる状態
            rend.material.color = ParryColor;
        }
    }

    IEnumerator AIBehaviour()
    {
        while (true)
        {
            // 動く状態
            rend.material.color = moveColor;
            // 正規化（念のため）
            Vector3 dir = moveDirection.normalized;

            // 動く時間
            float moveTime = Random.Range(moveTimeMin, moveTimeMax);
            float timer = 0f;


            while (timer < moveTime)
            {
                transform.position += dir * speed * Time.deltaTime;
                timer += Time.deltaTime;
                yield return null;
            }

            // 止まる時間
            float stopTime = Random.Range(stopTimeMin, stopTimeMax);
            rend.material.color = stopColor;
            //   Push();
            timer = 0f;

            while (timer < stopTime)
            {
                Push(); // 毎フレーム確認
                timer += Time.deltaTime * 1.2f;
                yield return null;
            }
            yield return new WaitForSeconds(stopTime);

        }
    }
}
