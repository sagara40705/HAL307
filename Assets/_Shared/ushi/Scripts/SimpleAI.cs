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
    [SerializeField] public Light spotLightPrefab;

    public Vector3 spawnPosition = new Vector3(0, 3, 16);
    [SerializeField] public float spotAngleSpeed = 30.0f;
    [SerializeField] public float rotateStep = 30.0f;
    private Light currentSpotLight;

    [SerializeField] private float spotStep = 5.0f;
    public GameObject lightObj;
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
                }

                if (Keyboard.current.cKey.wasPressedThisFrame)
                {
                    Debug.Log("Push!");

                    isParry = true;
                    rend.material.color = ParryColor;
                }

                break;

            case 1:

                if (!isParry)
                {
                    rend.material.color = PowerColor;
                }

                if (Keyboard.current.dKey.wasPressedThisFrame)
                {
                    Debug.Log("Push!");
                    isParry = true;
                    rend.material.color = ParryColor;
                }

                break;

            case 2:

                if (!isParry)
                {
                    rend.material.color = MasssuruColor;
                }

                if (Keyboard.current.fKey.wasPressedThisFrame)
                {
                    Debug.Log("Push!");
                    isParry = true;
                    rend.material.color = ParryColor;
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
                // 動き始めたらライト削除
                LightDelete();
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
                //止まる前にLightを呼び出し
                //CreateSpotLight();
                Push(); // 毎フレーム確認
                timer += Time.deltaTime * 1.2f;
                yield return null;
            }
            CreateSpotLight();
            yield return new WaitForSeconds(stopTime);

        }
    }
    void CreateSpotLight()
    {
        // 新しくスポットライト用のGameObjectを作成する
        lightObj = new GameObject("CurrentSpotLight");

        // スポットライトを出す位置と向きを設定する
        lightObj.transform.position = transform.position + new Vector3(0, 2, 0); ;
        lightObj.transform.rotation = Quaternion.Euler(90, 0, 0);

        // 作成したGameObjectにLightコンポーネントを追加する
        currentSpotLight = lightObj.AddComponent<Light>();

        // ライトの種類と明るさ、届く距離を設定する
        currentSpotLight.type = LightType.Spot;
        currentSpotLight.intensity = 10f;
        currentSpotLight.range = 20f;

        // スポットライトが影を落とすように設定する
        currentSpotLight.shadows = LightShadows.Soft;
        currentSpotLight.shadowStrength = 1.0f;
        currentSpotLight.shadowBias = 0.05f;
        currentSpotLight.shadowNormalBias = 0.4f;

        // スポットライトの外側と内側の角度を設定する
        currentSpotLight.spotAngle = 60f;
        currentSpotLight.innerSpotAngle = 30f;
    }
    void LightDelete()
    {
        Destroy(lightObj);
      //Destroy(spotLightPrefab);
    }
}
