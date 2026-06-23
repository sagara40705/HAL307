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
        [SerializeField] public float innerAngleSpeed = 30.0f;
        private Light currentSpotLight;

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
                currentSpotLight.spotAngle -= spotAngleSpeed * Time.deltaTime;
            }

            if (Keyboard.current.dKey.wasPressedThisFrame)
            {
                Debug.Log("d");
                currentSpotLight.spotAngle += spotAngleSpeed * Time.deltaTime;
            }

            //-----------------------------------
            // 上下キー
            // Inner Spot Angle
            //-----------------------------------
            if (Keyboard.current.wKey.wasPressedThisFrame)
            {
                Debug.Log("w");
                currentSpotLight.spotAngle -= innerAngleSpeed * Time.deltaTime;
            }

            if (Keyboard.current.sKey.wasPressedThisFrame)
            {
                Debug.Log("s");
                currentSpotLight.spotAngle -= innerAngleSpeed * Time.deltaTime;
            }
        }

        void CreateSpotLight()
        {
            GameObject lightObj = new GameObject("CurrentSpotLight");

            lightObj.transform.position = spawnPosition;
            lightObj.transform.rotation = Quaternion.Euler(90, 0, 0);

            currentSpotLight = lightObj.AddComponent<Light>();

            currentSpotLight.type = LightType.Spot;
            currentSpotLight.intensity = 10f;
            currentSpotLight.range = 20f;

            currentSpotLight.spotAngle = 60f;
            currentSpotLight.innerSpotAngle = 30f;
        }

    }
}
