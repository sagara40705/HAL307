using Unity.InferenceEngine;
using UnityEngine;
using System.Text;

public class VoiceOnnxRunner : MonoBehaviour
{
    [Header("ONNXモデル")]
    [SerializeField] private ModelAsset modelAsset;

    [Header("参照")]
    [SerializeField] private MicrophoneInput microphoneInput;
    [SerializeField] private MfccFeatureExtractor featureExtractor;
    [SerializeField] private VoiceCommandAction voiceCommandAction;

    [Header("推論設定")]
    [SerializeField] private float inferenceInterval = 0.2f;
    [SerializeField] private float minVolumeThreshold = 0.0001f;
    [SerializeField] private bool logPrediction = true;
    [SerializeField] private bool logScores = true;

    [Header("誤爆防止")]
    [SerializeField] private bool useSafetyFilter = true;
    [SerializeField] private float muscleNoneMargin = 10.0f;
    [SerializeField] private float powerNoneMargin = 10.0f;
    [SerializeField] private float yaaNoneMargin = 10.0f;

    private Worker worker;
    private Model runtimeModel;
    private float timer = 0f;

    // Python側の labels 順に合わせる
    private readonly string[] labels = { "muscle", "none", "power", "yaa" };

    private void Start()
    {
        if (modelAsset == null)
        {
            Debug.LogError("Model Asset が設定されていません");
            return;
        }

        if (microphoneInput == null)
        {
            Debug.LogError("MicrophoneInput が設定されていません");
            return;
        }

        if (featureExtractor == null)
        {
            Debug.LogError("MfccFeatureExtractor が設定されていません");
            return;
        }

        runtimeModel = ModelLoader.Load(modelAsset);
        worker = new Worker(runtimeModel, BackendType.CPU);

        Debug.Log("ONNXモデル読み込み成功");
    }

    private void Update()
    {
        if (worker == null) return;

        timer += Time.deltaTime;
        if (timer >= inferenceInterval)
        {
            timer = 0f;
            RunInference();
        }
    }

    private void RunInference()
    {
        float[] samples = microphoneInput.GetLatestSamples();
        if (samples == null || samples.Length == 0)
        {
            Debug.LogWarning("音声サンプルが取れていません");
            return;
        }

        float volume = GetMaxAbs(samples);
        if (volume < minVolumeThreshold)
        {
            return;
        }

        float[,] mfcc = featureExtractor.ExtractDummyMfcc(samples);

        using Tensor<float> inputTensor = new Tensor<float>(new TensorShape(1, 1, 20, 32));

        for (int m = 0; m < 20; m++)
        {
            for (int t = 0; t < 32; t++)
            {
                inputTensor[0, 0, m, t] = mfcc[m, t];
            }
        }

        worker.Schedule(inputTensor);

        Tensor<float> output = worker.PeekOutput() as Tensor<float>;
        if (output == null)
        {
            Debug.LogWarning("推論結果が取得できませんでした");
            return;
        }

        Tensor<float> readable = output.ReadbackAndClone() as Tensor<float>;
        if (readable == null)
        {
            Debug.LogWarning("推論結果の読み取りに失敗しました");
            return;
        }

        float muscleScore = readable[0];
        float noneScore = readable[1];
        float powerScore = readable[2];
        float yaaScore = readable[3];

        int predictedIndex = ArgMax(readable);
        string predictedLabel = labels[predictedIndex];

        if (logPrediction)
        {
            Debug.Log($"推論結果: {predictedLabel} / volume={volume:F3}");
        }

        if (logScores)
        {
            Debug.Log(BuildScoreLog(readable));
        }

        if (useSafetyFilter)
        {
            if (predictedLabel == "muscle" && muscleScore - noneScore < muscleNoneMargin)
            {
                Debug.Log($"muscle を無視しました（none と僅差: diff={muscleScore - noneScore:F3}）");
                readable.Dispose();
                return;
            }

            if (predictedLabel == "power" && powerScore - noneScore < powerNoneMargin)
            {
                Debug.Log($"power を無視しました（none と僅差: diff={powerScore - noneScore:F3}）");
                readable.Dispose();
                return;
            }

            if (predictedLabel == "yaa" && yaaScore - noneScore < yaaNoneMargin)
            {
                Debug.Log($"yaa を無視しました（none と僅差: diff={yaaScore - noneScore:F3}）");
                readable.Dispose();
                return;
            }
        }

        if (voiceCommandAction != null)
        {
            voiceCommandAction.ExecuteCommand(predictedLabel);
        }

        readable.Dispose();
    }

    private float GetMaxAbs(float[] samples)
    {
        float maxAbs = 0f;

        for (int i = 0; i < samples.Length; i++)
        {
            float v = Mathf.Abs(samples[i]);
            if (v > maxAbs)
            {
                maxAbs = v;
            }
        }

        return maxAbs;
    }

    private int ArgMax(Tensor<float> tensor)
    {
        int maxIndex = 0;
        float maxValue = tensor[0];

        for (int i = 1; i < tensor.shape.length; i++)
        {
            if (tensor[i] > maxValue)
            {
                maxValue = tensor[i];
                maxIndex = i;
            }
        }

        return maxIndex;
    }

    private string BuildScoreLog(Tensor<float> tensor)
    {
        StringBuilder sb = new StringBuilder();
        sb.Append("scores: ");

        int count = Mathf.Min(labels.Length, tensor.shape.length);
        for (int i = 0; i < count; i++)
        {
            sb.Append(labels[i]);
            sb.Append("=");
            sb.Append(tensor[i].ToString("F4"));

            if (i < count - 1)
            {
                sb.Append(", ");
            }
        }

        return sb.ToString();
    }

    private void OnDisable()
    {
        worker?.Dispose();
    }
}