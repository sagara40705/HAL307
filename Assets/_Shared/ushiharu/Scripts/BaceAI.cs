using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem;
using ushiharu.Sandbox;
using UnityEngine.InputSystem.Switch;



namespace ushiharu.Sandbox
{

    public class BaceAI : CharacterBase
    {

        int[] Poose = new int [5];
        int randomPoose;
        public Vector3 moveDirection= Vector3.forward;
        private Vector3 lastPosition;
        public Light targetLight;
        public GameObject lightObj;
       [SerializeField] public Light spotLightPrefab;

        public Vector3 spawnPosition = new Vector3(0, 3, 16);
        [SerializeField] public float spotAngleSpeed = 30.0f;
        [SerializeField] public float rotateStep = 30.0f;
        private Light currentSpotLight;

        [SerializeField] private float spotStep = 5.0f;
        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            lastPosition = transform.position;
            // 最初は消す
            targetLight.enabled = false;
            StartCoroutine(AIBehaviour());
            rend = GetComponent<Renderer>();
        }
        void Update()
        {

        }

        //コルーチンとは
        //yield return を使った処理を停止するプログラムである
        //この場合は指定した時間処理を止める事によって
        //ポーズ、パリィの時間として使っている
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
                    //動き出す時間
                    while (timer <= movetime)
                    {
                        // 動き始めたらライト削除
                        LightDelete();
                        transform.position += dir * speed * Time.deltaTime;
                        Debug.Log("P!");
                        AiState();
                        timer += Time.deltaTime * 1.5f;
                        yield return null;
                    }
                }
               //止まる前にLightを呼び出し
                CreateSpotLight();
                //止まる時間をコルーチンで返している
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
        }

    }

}
