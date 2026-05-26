using UnityEngine;

public class MicrophoneDebug : MonoBehaviour

{

    [SerializeField] private MicrophoneInput microphoneInput;

    [SerializeField] private float checkInterval = 1.0f;

    private float timer = 0f;

    private void Update()

    {

        timer += Time.deltaTime;

        if (timer >= checkInterval)

        {

            timer = 0f;

            if (microphoneInput == null)

            {

               Debug.LogWarning("MicrophoneInput が設定されていません");

                return;

            }

            float[] samples = microphoneInput.GetLatestSamples();

            if (samples == null || samples.Length == 0)

            {

                Debug.LogWarning("サンプルが取れていません");

                return;

            }

            float maxAbs = 0f;

            for (int i = 0; i < samples.Length; i++)

            {

                float v = Mathf.Abs(samples[i]);

                if (v > maxAbs)

                {

                    maxAbs = v;

                }

            }

            Debug.Log("マイク入力 最大音量: " + maxAbs);

        }

    }

}
