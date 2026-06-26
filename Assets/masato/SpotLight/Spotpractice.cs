using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

namespace masato.Sandbox
{

    public class Spot : MonoBehaviour
    {
        [SerializeField] public Light spotLightPrefab;

        public Vector3 spawnPosition = new Vector3(0, 3, 16);

        [SerializeField] public float spotAngleSpeed = 30.0f;
        [SerializeField] public float rotateStep = 30.0f;
        private Light currentSpotLight;

        [SerializeField] private float spotStep = 5.0f;

        // Start is called once before the first execution of Update after the MonoBehaviour is created
        void Start()
        {
            
        }

        // Update is called once per frame
        void Update()
        {
            //-----------------------------------
            // Enterで生成
            //-----------------------------------
            if (Keyboard.current.enterKey.wasPressedThisFrame)
              
            {
                CreateSpotLight();
            }


            //-----------------------------------
            // 左右キー
            // Spot Angle
            //-----------------------------------
            if (Keyboard.current.aKey.wasPressedThisFrame)
            {
                Debug.Log("a");
                currentSpotLight.spotAngle -= spotAngleSpeed;
            }

            if (Keyboard.current.dKey.wasPressedThisFrame)
            {
                Debug.Log("d");
                currentSpotLight.spotAngle += spotAngleSpeed;
            }

            //-----------------------------------
            // 上下キー
            // Inner Spot Angle
            //-----------------------------------
            if (Keyboard.current.wKey.wasPressedThisFrame)
            {
                currentSpotLight.transform.Rotate(-rotateStep, 0f, 0f, Space.Self);
            }

            if (Keyboard.current.sKey.wasPressedThisFrame)
            {
                currentSpotLight.transform.Rotate(rotateStep, 0f, 0f, Space.Self);
            }


        }

        void CreateSpotLight()
        {
            // 新しくスポットライト用のGameObjectを作成する
            GameObject lightObj = new GameObject("CurrentSpotLight");

            // スポットライトを出す位置と向きを設定する
            lightObj.transform.position = spawnPosition;
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

    }
}
