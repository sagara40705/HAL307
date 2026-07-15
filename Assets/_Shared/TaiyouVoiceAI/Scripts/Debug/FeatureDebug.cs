using UnityEngine;

public class FeatureDebug : MonoBehaviour
{
    [SerializeField] private MicrophoneInput microphoneInput;
    [SerializeField] private MfccFeatureExtractor featureExtractor;
    [SerializeField] private float checkInterval = 1.5f;

    private float timer = 0f;

    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= checkInterval)
        {
            timer = 0f;

            if (microphoneInput == null || featureExtractor == null)
            {
                Debug.LogWarning("必要な参照が未設定です");
                return;
            }

            float[] samples = microphoneInput.GetLatestSamples();
            if (samples == null || samples.Length == 0)
            {
                Debug.LogWarning("音声サンプルが取れていません");
                return;
            }

            float[,] mfcc = featureExtractor.ExtractDummyMfcc(samples);

            Debug.Log("特徴量作成成功: " + mfcc.GetLength(0) + " x " + mfcc.GetLength(1));
        }
    }
}