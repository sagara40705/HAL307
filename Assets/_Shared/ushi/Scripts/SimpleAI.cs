using UnityEngine;
using System.Collections;

public class SimpleAI : MonoBehaviour
{
    public float speed = 3f;
    public float moveTimeMin = 2f;
    public float moveTimeMax = 5f;
    public float stopTimeMin = 1f;
    public float stopTimeMax = 3f;
    // 色の設定
    //ポーズ種類
    public Color moveColor = Color.white; // 動いてるとき
    //ポーズしている時の種類
    public Color stopColor = Color.red;
    public Color PowerColor = Color.yellow;
    public Color MasssuruColor = Color.black;

    public Color ParryColor = Color.blue;  // パリィ状態のとき

    // ← ここで方向を決める（例：前方向）
    public Vector3 moveDirection = Vector3.forward;
    private Renderer rend;
    private Color[] colors;
    private int index;
    void Start()
    {

        rend = GetComponent<Renderer>();
        colors = new Color[]
        {
            stopColor,
            PowerColor,
            MasssuruColor,
            ParryColor,//ここはフェイク
        };
        StartCoroutine(AIBehaviour());
    }

    void Push()
    {

        switch (index)
        {
            case 1:
                rend.material.color = stopColor;
                break;
            case 2:
                rend.material.color = PowerColor;
                break;
            case 3:
                rend.material.color = MasssuruColor;
                break;
        }
        //if (Keyboard.current.cKey.wasPressedThisFrame)
        //{
        //    Debug.Log("Push!");
        //    止まる状態
        //    rend.material.color = ParryColor;
        //}
    }

    IEnumerator AIBehaviour()
    {
        while (true)
        {
            // 動く状態
            rend.material.color = moveColor;
            // 正規化（念のため）
            Vector3 dir = moveDirection.normalized;
            //Startで作った配列を要素数から-1された範囲でランダムに選ぶ
            index = UnityEngine.Random.Range(0, colors.Length);
            // 動く時間
            float moveTime = UnityEngine.Random.Range(moveTimeMin, moveTimeMax);
            float timer = 0f;


            while (timer < moveTime)
            {
                transform.position += dir * speed * Time.deltaTime;
                timer += Time.deltaTime;
                yield return null;
            }

            // 止まる時間
            float stopTime = UnityEngine.Random.Range(stopTimeMin, stopTimeMax);
            rend.material.color = colors[index];
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
