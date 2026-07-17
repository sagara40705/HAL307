using Unity.InferenceEngine;
using UnityEngine;
using System.Text;
using UnityEngine.InputSystem;

public class VoiceOnnxRunner : MonoBehaviour
{
    private const string DebugWindowName = "Voice ONNX";
    private const int ModelMfccCount = 20;
    private const int ModelTargetWidth = 32;

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
    private readonly float[] latestScores = new float[4];
    private string initializationStatus = "Not initialized";
    private string inferenceStatus = "Waiting for initialization";
    private string latestLabel = "-";
    private float latestInputVolume;
    private int inferenceCount;
    private bool hasInferenceResult;

    // Python側の labels 順に合わせる
    private readonly string[] labels = { "muscle", "none", "power", "yaa" };

    private void Start()
    {
        CreateDebugMenu();
        initializationStatus = "Initializing";

        if (modelAsset == null)
        {
            initializationStatus = "ModelAsset is missing";
            Debug.LogError("Model Asset が設定されていません");
            return;
        }

        if (microphoneInput == null)
        {
            initializationStatus = "MicrophoneInput is missing";
            Debug.LogError("MicrophoneInput が設定されていません");
            return;
        }

        if (featureExtractor == null)
        {
            initializationStatus = "MfccFeatureExtractor is missing";
            Debug.LogError("MfccFeatureExtractor が設定されていません");
            return;
        }

        runtimeModel = ModelLoader.Load(modelAsset);
        worker = new Worker(runtimeModel, BackendType.CPU);
        initializationStatus = "Ready";
        inferenceStatus = "Waiting for microphone input";

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


        //// Vキー押している間だけ動作
        //if (!DebugShortcuts.Held(Key.V))
        //{
        //    state = VoiceProcessState.Idle;
        //    timer = 0f;
        //    return;
        //}

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
            latestInputVolume = 0f;
            inferenceStatus = "Waiting for microphone samples";
            state = VoiceProcessState.Idle;
            return;
        }

        cachedVolume = GetMaxAbs(cachedSamples);
        latestInputVolume = cachedVolume;

        if (cachedVolume < minVolumeThreshold)
        {
            inferenceStatus = $"Volume below threshold ({cachedVolume:F6} < {minVolumeThreshold:F6})";
            state = VoiceProcessState.Idle;
            return;
        }

        inferenceStatus = "Creating MFCC";
        state = VoiceProcessState.MakeFeature;
    }

    private void MakeFeatureStep()
    {
        cachedMfcc = featureExtractor.ExtractMfcc(cachedSamples, ModelMfccCount, ModelTargetWidth);
        inferenceStatus = "Running ONNX";
        state = VoiceProcessState.RunOnnx;
    }

    private void RunOnnxStep()
    {
        using Tensor<float> inputTensor =
            new Tensor<float>(new TensorShape(1, 1, ModelMfccCount, ModelTargetWidth));

        for (int m = 0; m < ModelMfccCount; m++)
        {
            for (int t = 0; t < ModelTargetWidth; t++)
            {
                inputTensor[0, 0, m, t] = cachedMfcc[m, t];
            }
        }

        worker.Schedule(inputTensor);

        Tensor<float> output = worker.PeekOutput() as Tensor<float>;
        if (output == null)
        {
            inferenceStatus = "ONNX output is unavailable";
            state = VoiceProcessState.Idle;
            return;
        }

        cachedOutput = output.ReadbackAndClone() as Tensor<float>;

        if (cachedOutput == null)
        {
            inferenceStatus = "ONNX output readback failed";
            state = VoiceProcessState.Idle;
            return;
        }

        int predictedIndex = ArgMax(cachedOutput);
        cachedLabel = labels[predictedIndex];
        StoreInferenceDebugSnapshot(cachedOutput, cachedLabel, cachedVolume);
        inferenceStatus = $"Completed: {cachedLabel}";

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

        if (voiceCommandAction != null)
        {
            string commandToExecute =
                cachedLabel == "none" ? "yaa" : cachedLabel;

            voiceCommandAction.ExecuteCommand(
                commandToExecute,
                cachedVolume
            );
        }

        cachedOutput?.Dispose();
        cachedOutput = null;

        cachedSamples = null;
        cachedMfcc = null;
        cachedLabel = null;

        state = VoiceProcessState.Idle;
    }


    private void RunInferenceWithSamples(float[] samples, float volume)
    {
        float[,] mfcc = featureExtractor.ExtractMfcc(samples, ModelMfccCount, ModelTargetWidth);

        using Tensor<float> inputTensor =
            new Tensor<float>(new TensorShape(1, 1, ModelMfccCount, ModelTargetWidth));

        for (int m = 0; m < ModelMfccCount; m++)
        {
            for (int t = 0; t < ModelTargetWidth; t++)
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
        StoreInferenceDebugSnapshot(readable, predictedLabel, volume);

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

    private void CreateDebugMenu()
    {
        RuntimeDebug.DebugWindow window = RuntimeDebug.DebugMenu.CreateWindow(DebugWindowName);
        window.CreateMessage("Status", () => initializationStatus);
        window.CreateMessage("Inference Status", () => inferenceStatus);
        window.CreateMessage("Process State", () => state.ToString());
        window.CreateMessage("Input Volume", () => latestInputVolume.ToString("F6"));
        window.CreateMessage("Prediction", () => hasInferenceResult ? latestLabel : "-");
        window.CreateMessage("Inference Count", () => inferenceCount.ToString());

        for (int i = 0; i < labels.Length; i++)
        {
            int scoreIndex = i;
            window.CreateMessage($"Score {labels[scoreIndex]}", () => GetLatestScoreText(scoreIndex));
        }
    }

    private void StoreInferenceDebugSnapshot(Tensor<float> output, string predictedLabel, float inputVolume)
    {
        if (output == null)
        {
            return;
        }

        latestLabel = string.IsNullOrEmpty(predictedLabel) ? "-" : predictedLabel;
        latestInputVolume = inputVolume;

        int scoreCount = Mathf.Min(latestScores.Length, output.shape.length);
        for (int i = 0; i < latestScores.Length; i++)
        {
            latestScores[i] = i < scoreCount ? output[i] : 0f;
        }

        inferenceCount++;
        hasInferenceResult = true;
    }

    private string GetLatestScoreText(int index)
    {
        if (!hasInferenceResult || index < 0 || index >= latestScores.Length)
        {
            return "-";
        }

        return latestScores[index].ToString("F4");
    }

    private void OnDisable()
    {
        worker?.Dispose();
        worker = null;
        cachedOutput?.Dispose();
        cachedOutput = null;
        initializationStatus = "Disabled";
        inferenceStatus = "Disabled";
    }

    private void OnDestroy()
    {
        RuntimeDebug.DebugMenu.DestroyWindow(DebugWindowName);
    }
}
