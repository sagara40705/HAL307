using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.InferenceEngine;
using UnityEngine;
using UnityEngine.InputSystem;

public class VoiceOnnxRunner_Menu : MonoBehaviour
{
    [Header("ONNX Model")]
    [SerializeField] private ModelAsset modelAsset;

    [Header("References")]
    [SerializeField] private TextAsset labelsAsset;
    [SerializeField] private MicrophoneInput microphoneInput;
    [SerializeField] private MfccFeatureExtractor featureExtractor;
    [SerializeField] private MenuVoiceCommandAction menuVoiceCommandAction;

    [Header("Inference")]
    [SerializeField] private float inferenceInterval = 0.2f;
    [SerializeField] private float minVolumeThreshold = 0.001f;
    [SerializeField] private bool requireHoldToTalk = false;
    [SerializeField] private Key holdToTalkKey = Key.V;
    [SerializeField] private bool logPrediction = true;
    [SerializeField] private bool logScores = false;

    [Header("Command Filter")]
    [SerializeField] private bool ignoreNone = true;
    [SerializeField] private float commandCooldown = 0.6f;

    private readonly string[] fallbackLabels = { "ue", "sita", "kettei", "time", "none" };

    private Worker worker;
    private Model runtimeModel;
    private string[] labels;
    private float timer;
    private float lastCommandTime = -999f;
    private string lastCommand = "";

    private void Start()
    {
        labels = LoadLabels();

        if (modelAsset == null)
        {
            Debug.LogError("VoiceOnnxRunner_Menu: modelAsset が設定されていません。");
            enabled = false;
            return;
        }

        if (microphoneInput == null)
        {
            microphoneInput = GetComponent<MicrophoneInput>();
        }

        if (featureExtractor == null)
        {
            featureExtractor = GetComponent<MfccFeatureExtractor>();
        }

        if (menuVoiceCommandAction == null)
        {
            menuVoiceCommandAction = GetComponent<MenuVoiceCommandAction>();
        }

        if (microphoneInput == null || featureExtractor == null)
        {
            Debug.LogError("VoiceOnnxRunner_Menu: MicrophoneInput または MfccFeatureExtractor が見つかりません。");
            enabled = false;
            return;
        }

        if (menuVoiceCommandAction == null)
        {
            Debug.LogWarning("VoiceOnnxRunner_Menu: MenuVoiceCommandAction が設定されていません。音声結果だけログに出ます。");
        }

        runtimeModel = ModelLoader.Load(modelAsset);
        worker = new Worker(runtimeModel, BackendType.CPU);

        Debug.Log("VoiceOnnxRunner_Menu ready. labels=" + string.Join(", ", labels));
    }

    private void Update()
    {
        if (worker == null)
        {
            return;
        }

        if (requireHoldToTalk && !IsHoldToTalkPressed())
        {
            timer = 0f;
            return;
        }

        timer += Time.unscaledDeltaTime;
        if (timer < inferenceInterval)
        {
            return;
        }

        timer = 0f;
        RunInference();
    }

    private bool IsHoldToTalkPressed()
    {
        return Keyboard.current != null && Keyboard.current[holdToTalkKey].isPressed;
    }

    private void RunInference()
    {
        float[] samples = microphoneInput.GetLatestSamples();
        if (samples == null || samples.Length == 0)
        {
            return;
        }

        float volume = GetMaxAbs(samples);
        if (volume < minVolumeThreshold)
        {
            return;
        }

        float[,] mfcc = featureExtractor.ExtractMfcc(samples);

        using Tensor<float> inputTensor = new Tensor<float>(
            new TensorShape(1, 1, MfccFeatureExtractor.MfccCount, MfccFeatureExtractor.TargetWidth)
        );

        for (int m = 0; m < MfccFeatureExtractor.MfccCount; m++)
        {
            for (int t = 0; t < MfccFeatureExtractor.TargetWidth; t++)
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

        using Tensor<float> readable = output.ReadbackAndClone() as Tensor<float>;
        if (readable == null)
        {
            return;
        }

        int predictedIndex = ArgMax(readable);
        if (predictedIndex < 0 || predictedIndex >= labels.Length)
        {
            Debug.LogWarning($"VoiceOnnxRunner_Menu: predicted index {predictedIndex} is outside labels length {labels.Length}.");
            return;
        }

        string predictedLabel = labels[predictedIndex].Trim().ToLowerInvariant();

        if (logPrediction)
        {
            Debug.Log($"[Menu Voice AI] command={predictedLabel} volume={volume:F4}");
        }

        if (logScores)
        {
            Debug.Log(BuildScoreLog(readable));
        }

        if (ignoreNone && predictedLabel == "none")
        {
            return;
        }

        if (IsCooldown(predictedLabel))
        {
            return;
        }

        lastCommand = predictedLabel;
        lastCommandTime = Time.unscaledTime;

        if (menuVoiceCommandAction != null)
        {
            menuVoiceCommandAction.ExecuteCommand(predictedLabel, volume);
        }
    }

    private bool IsCooldown(string command)
    {
        if (Time.unscaledTime - lastCommandTime < commandCooldown && lastCommand == command)
        {
            return true;
        }

        return false;
    }

    private string[] LoadLabels()
    {
        string source = labelsAsset != null ? labelsAsset.text : string.Empty;

        if (string.IsNullOrWhiteSpace(source))
        {
            string modelFolderLabels = Path.Combine(Application.dataPath, "Models", "voice_labels.txt");
            if (File.Exists(modelFolderLabels))
            {
                source = File.ReadAllText(modelFolderLabels);
            }
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            return fallbackLabels;
        }

        List<string> parsed = new List<string>();
        using StringReader reader = new StringReader(source);
        while (reader.ReadLine() is { } line)
        {
            string label = line.Trim();
            if (!string.IsNullOrEmpty(label))
            {
                parsed.Add(label);
            }
        }

        return parsed.Count > 0 ? parsed.ToArray() : fallbackLabels;
    }

    private float GetMaxAbs(float[] samples)
    {
        float maxAbs = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            maxAbs = Mathf.Max(maxAbs, Mathf.Abs(samples[i]));
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
        StringBuilder sb = new StringBuilder("scores: ");
        int count = Mathf.Min(labels.Length, tensor.shape.length);

        for (int i = 0; i < count; i++)
        {
            if (i > 0)
            {
                sb.Append(", ");
            }

            sb.Append(labels[i]);
            sb.Append('=');
            sb.Append(tensor[i].ToString("F4"));
        }

        return sb.ToString();
    }

    private void OnDisable()
    {
        worker?.Dispose();
        worker = null;
    }

    public void SetMinVolumeThreshold(float value)
    {
        minVolumeThreshold = value;
    }

    public float GetMinVolumeThreshold()
    {
        return minVolumeThreshold;
    }
}
