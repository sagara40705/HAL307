using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

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
    //色を取得する箱
    private Renderer rend;
    //色を取得するための配列
    private Color[] colors;
    //現在のポーズの種類を保存する変数(色)
    private int index;
    //ポーズ中パリィが発生したかどうか
    private bool isParry = false;
    private  float PlayerPoint;
    private float EnemyPoint;
    public int Powerpoint;
    public int Massurupoint;
    public int Yaapoint;

    void Start()
    {

        rend = GetComponent<Renderer>();
        colors = new Color[]
        {
            stopColor,
            PowerColor,
            MasssuruColor,
        };
        StartCoroutine(AIBehaviour());
    }
 
    void Push()
    {

        switch (index)
        {
            case 0:

                if (!isParry)
                {
                    rend.material.color = stopColor;
                    //敵のポイント取得
                    EnemyPoint += 1f;
                }
                ///ここにプレイヤーが何の声を出したかの条件文
                if (Keyboard.current.cKey.wasPressedThisFrame)
                {
                    Debug.Log("Push!");

                    isParry = true;
                    rend.material.color = ParryColor;
                    //敵のポイント取得
                    //PlayerPoint += 1f;
                    PowerPoint(Powerpoint);
                }

                break;

            case 1:

                if (!isParry)
                {
                    rend.material.color = PowerColor;
                    //敵のポイント取得
                    EnemyPoint += 1f;
                }
                ///ここにプレイヤーが何の声を出したかの条件文
                if (Keyboard.current.dKey.wasPressedThisFrame)
                {
                    Debug.Log("Push!");
                    isParry = true;
                    rend.material.color = ParryColor;
                    //PlayerPoint += 1f;
                    MassuruPoint(Massurupoint);
                }

                break;

            case 2:

                if (!isParry)
                {
                    rend.material.color = MasssuruColor;
                    EnemyPoint += 1f;
                }
                ///ここにプレイヤーが何の声を出したかの条件文
                if (Keyboard.current.fKey.wasPressedThisFrame)
                {
                    Debug.Log("Push!");
                    isParry = true;
                    rend.material.color = ParryColor;
                    //PlayerPoint += 1f;
                    YaaPoint(Yaapoint);
                }

                break;
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

            //if (Random.value<=0.6)
            //{
            while (timer < stopTime)
            {
                Push(); // 毎フレーム確認
                timer += Time.deltaTime * 1.2f;
                yield return null;
            }
           // }

            yield return new WaitForSeconds(stopTime);

        }
    }

    public int PowerPoint(int powerpoint)
    {
        powerpoint++;
        return powerpoint;

    }

    public int MassuruPoint(int massurupoint)
    {
        massurupoint++;
        return massurupoint;
    }
    public int YaaPoint(int yaapoint)
    {
        yaapoint++;
        return yaapoint;
    }
}
