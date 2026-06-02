using Unity.InferenceEngine;
using UnityEngine;
using System.Text;
using UnityEngine.InputSystem;

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
    [SerializeField] private float muscleNoneMargin = 3.0f;
    [SerializeField] private float powerNoneMargin = 10.0f;
    [SerializeField] private float yaaNoneMargin = 5.0f;

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

    private enum VoiceProcessState
    {
        Idle,
        CheckVolume,
        MakeFeature,
        RunOnnx,
        HandleResult
    }

    private VoiceProcessState state = VoiceProcessState.Idle;

    private float[] cachedSamples;
    private float cachedVolume;
    private float[,] cachedMfcc;
    private Tensor<float> cachedOutput;
    private string cachedLabel;

    private void Update()
    {
        if (worker == null) return;

        // Vキー押している間だけ動作
        if (!DebugShortcuts.Held(Key.V))
        {
            state = VoiceProcessState.Idle;
            timer = 0f;
            return;
        }

        // Idle中のみタイマー進行
        if (state == VoiceProcessState.Idle)
        {
            timer += Time.deltaTime;

            if (timer >= inferenceInterval)
            {
                timer = 0f;

                Debug.Log("[Voice] 推論開始");

                state = VoiceProcessState.CheckVolume;
            }

            return;
        }

        // 分割処理
        switch (state)
        {
            case VoiceProcessState.CheckVolume:
                CheckVolumeStep();
                break;

            case VoiceProcessState.MakeFeature:
                MakeFeatureStep();
                break;

            case VoiceProcessState.RunOnnx:
                RunOnnxStep();
                break;

            case VoiceProcessState.HandleResult:
                HandleResultStep();
                break;
        }
    }

    private void CheckVolumeStep()
    {
        cachedSamples = microphoneInput.GetLatestSamples();

        if (cachedSamples == null || cachedSamples.Length == 0)
        {
            state = VoiceProcessState.Idle;
            return;
        }

        cachedVolume = GetMaxAbs(cachedSamples);

        if (cachedVolume < minVolumeThreshold)
        {
            state = VoiceProcessState.Idle;
            return;
        }

        state = VoiceProcessState.MakeFeature;
    }

    private void MakeFeatureStep()
    {
        cachedMfcc = featureExtractor.ExtractDummyMfcc(cachedSamples);
        state = VoiceProcessState.RunOnnx;
    }

    private void RunOnnxStep()
    {
        using Tensor<float> inputTensor =
            new Tensor<float>(new TensorShape(1, 1, 20, 32));

        for (int m = 0; m < 20; m++)
        {
            for (int t = 0; t < 32; t++)
            {
                inputTensor[0, 0, m, t] = cachedMfcc[m, t];
            }
        }

        worker.Schedule(inputTensor);

        Tensor<float> output = worker.PeekOutput() as Tensor<float>;
        if (output == null)
        {
            state = VoiceProcessState.Idle;
            return;
        }

        cachedOutput = output.ReadbackAndClone() as Tensor<float>;

        if (cachedOutput == null)
        {
            state = VoiceProcessState.Idle;
            return;
        }

        int predictedIndex = ArgMax(cachedOutput);
        cachedLabel = labels[predictedIndex];

        state = VoiceProcessState.HandleResult;
    }

    private void HandleResultStep()
    {
        if (logPrediction)
        {
            Debug.Log($"[AI] result={cachedLabel} / volume={cachedVolume:F4}");
        }

        if (logScores && cachedOutput != null)
        {
            Debug.Log(BuildScoreLog(cachedOutput));
        }

        // none の場合は何もしない
        if (cachedLabel == "none")
        {
            cachedOutput?.Dispose();
            cachedOutput = null;

            cachedSamples = null;
            cachedMfcc = null;
            cachedLabel = null;

            state = VoiceProcessState.Idle;
            return;
        }

        // 認識結果を実行
        if (voiceCommandAction != null)
        {
            voiceCommandAction.ExecuteCommand(cachedLabel, cachedVolume);
        }

        // 後始末
        cachedOutput?.Dispose();
        cachedOutput = null;

        cachedSamples = null;
        cachedMfcc = null;
        cachedLabel = null;

        // 次の推論を受け付ける
        state = VoiceProcessState.Idle;
    }


    private void RunInferenceWithSamples(float[] samples, float volume)
    {
        float[,] mfcc = featureExtractor.ExtractDummyMfcc(samples);

        using Tensor<float> inputTensor =
            new Tensor<float>(new TensorShape(1, 1, 20, 32));

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
            return;
        }

        Tensor<float> readable =
            output.ReadbackAndClone() as Tensor<float>;

        if (readable == null)
        {
            return;
        }

        int predictedIndex = ArgMax(readable);
        string predictedLabel = labels[predictedIndex];

        Debug.Log("[AI] " + predictedLabel);

        if (voiceCommandAction != null)
        {
            voiceCommandAction.ExecuteCommand(predictedLabel, volume);
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
