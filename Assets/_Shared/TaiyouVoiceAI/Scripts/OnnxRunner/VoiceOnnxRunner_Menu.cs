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
    [SerializeField] private PythonCompatMfccFeatureExtractor featureExtractor;
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
    [SerializeField] private bool useSafetyFilter = true;
    [SerializeField] private float minimumTopScore = 0f;
    [SerializeField] private float minimumTop2Margin = 0f;
    [SerializeField] private float timeNoneMargin = 2.0f;
    [SerializeField] private float timeTop2Margin = 1.0f;
    [SerializeField] private int timeRequiredConsecutive = 2;

    private readonly string[] fallbackLabels = { "ue", "sita", "hidari", "migi", "kettei", "time", "none" };

    private Worker worker;
    private Model runtimeModel;
    private string[] labels;
    private float timer;
    private float lastCommandTime = -999f;
    private string lastCommand = "";
    private string pendingCommand = "";
    private int pendingCommandCount;
    private string acceptedConsecutiveText = "1/1";
    private int inferenceSessionGeneration;
    private bool inferenceSessionValid;
    private int inferenceStartRunnerGeneration = -1;
    private int inferenceStartMicrophoneGeneration = -1;

#if UNITY_EDITOR
    private const float ExpectedLabelMarkerDuration = 2.5f;
    private int menuVoiceTestMarkerId;
    private bool menuVoiceTestMarkerActive;
    private string menuVoiceTestExpectedLabel = "";
    private float menuVoiceTestMarkerEndTime;
    private string menuVoiceTestLastSafetyRejectReason = "";
    private string menuVoiceTestLastDiscardReason = "";
#endif

    private struct PredictionStats
    {
        public int Top1Index;
        public int Top2Index;
        public float Top1Score;
        public float Top2Score;
        public float Top2Margin;
        public float NoneScore;
        public float TimeScore;
    }

    private void OnEnable()
    {
        ResetTransientInferenceState();
        LogRunnerLifecycle("Enable");
        InitializeInferenceResources();
    }

    private void InitializeInferenceResources()
    {
        if (worker != null)
        {
            return;
        }

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
            featureExtractor = GetComponent<PythonCompatMfccFeatureExtractor>();
        }

        if (menuVoiceCommandAction == null)
        {
            menuVoiceCommandAction = GetComponent<MenuVoiceCommandAction>();
        }

        if (microphoneInput == null || featureExtractor == null)
        {
            Debug.LogError("VoiceOnnxRunner_Menu: MicrophoneInput または PythonCompatMfccFeatureExtractor が見つかりません。");
            enabled = false;
            return;
        }

        if (menuVoiceCommandAction == null)
        {
            Debug.LogWarning("VoiceOnnxRunner_Menu: MenuVoiceCommandAction が設定されていません。音声結果だけログに出ます。");
        }

        runtimeModel = ModelLoader.Load(modelAsset);
        worker = new Worker(runtimeModel, BackendType.CPU);
        inferenceSessionGeneration++;
        inferenceSessionValid = true;

        Debug.Log("VoiceOnnxRunner_Menu ready. labels=" + string.Join(", ", labels));
        LogRunnerLifecycle("Initialize");
    }

    private void Update()
    {
#if UNITY_EDITOR
        UpdateExpectedLabelTestMarker();
#endif

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
        Debug.Log($"[MenuVoiceTrace] RunInference entered. object={name}, scene={gameObject.scene.name}, minVolumeThreshold={minVolumeThreshold:F8}, menuVoiceCommandActionNull={menuVoiceCommandAction == null}");

        float[] samples = microphoneInput.GetLatestSamples();
        if (samples == null || samples.Length == 0)
        {
            Debug.Log($"[MenuVoiceTrace] RunInference return. reason=samples empty, samplesLength={(samples == null ? -1 : samples.Length)}");
#if UNITY_EDITOR
            LogMenuVoiceEvaluation(
                "not_run",
                default,
                0f,
                0f,
                0f,
                false,
                "SamplesUnavailable",
                false);
#endif
            return;
        }

        CaptureInferenceContext();

        float volume = GetMaxAbs(samples);
#if UNITY_EDITOR
        GetSampleStatistics(samples, out float meanAbs, out float rms);
#endif
        bool belowVolumeThreshold = volume < minVolumeThreshold;
        Debug.Log($"[MenuVoiceTrace] Volume check. volume={volume:F8}, minVolumeThreshold={minVolumeThreshold:F8}, belowThreshold={belowVolumeThreshold}");
        if (volume < minVolumeThreshold)
        {
            Debug.Log($"[MenuVoiceReject] label=unknown reason=VolumeThreshold required={minVolumeThreshold:F8} actual={volume:F8}");
            Debug.Log("[MenuVoiceTrace] RunInference return. reason=volume below minVolumeThreshold");
#if UNITY_EDITOR
            LogMenuVoiceEvaluation(
                "not_run",
                default,
                volume,
                meanAbs,
                rms,
                false,
                "VolumeThreshold",
                false);
#endif
            ClearInferenceContext();
            return;
        }

        float[,] mfcc = featureExtractor.ExtractMfcc(samples);

        using Tensor<float> inputTensor = new Tensor<float>(
            new TensorShape(1, 1, PythonCompatMfccFeatureExtractor.MfccCount, PythonCompatMfccFeatureExtractor.TargetWidth)
        );

        for (int m = 0; m < PythonCompatMfccFeatureExtractor.MfccCount; m++)
        {
            for (int t = 0; t < PythonCompatMfccFeatureExtractor.TargetWidth; t++)
            {
                inputTensor[0, 0, m, t] = mfcc[m, t];
            }
        }

        Debug.Log("[MenuVoiceTrace] worker.Schedule before");
        worker.Schedule(inputTensor);
        Debug.Log("[MenuVoiceTrace] worker.Schedule after");

        Tensor<float> output = worker.PeekOutput() as Tensor<float>;
        if (output == null)
        {
            Debug.Log("[MenuVoiceTrace] RunInference return. reason=output null");
#if UNITY_EDITOR
            LogMenuVoiceEvaluation(
                "not_run",
                default,
                volume,
                meanAbs,
                rms,
                false,
                "InferenceOutputUnavailable",
                false);
#endif
            ClearInferenceContext();
            return;
        }

        using Tensor<float> readable = output.ReadbackAndClone() as Tensor<float>;
        if (readable == null)
        {
            Debug.Log("[MenuVoiceTrace] RunInference return. reason=ReadbackAndClone failed");
#if UNITY_EDITOR
            LogMenuVoiceEvaluation(
                "not_run",
                default,
                volume,
                meanAbs,
                rms,
                false,
                "ReadbackFailed",
                false);
#endif
            ClearInferenceContext();
            return;
        }

        int predictedIndex = ArgMax(readable);
        if (readable.shape.length != labels.Length)
        {
            Debug.LogWarning($"VoiceOnnxRunner_Menu: ONNX output class count and labels count do not match. output={readable.shape.length}, labels={labels.Length}. Check model and labels order.");
        }

        if (predictedIndex < 0 || predictedIndex >= labels.Length)
        {
            Debug.LogWarning($"VoiceOnnxRunner_Menu: predicted index {predictedIndex} is outside labels length {labels.Length}.");
#if UNITY_EDITOR
            LogMenuVoiceEvaluation(
                "not_run",
                default,
                volume,
                meanAbs,
                rms,
                false,
                "PredictedIndexInvalid",
                false);
#endif
            ClearInferenceContext();
            return;
        }

        string predictedLabel = labels[predictedIndex].Trim().ToLowerInvariant();
        PredictionStats stats = GetPredictionStats(readable);

        if (DiscardIfInferenceContextChanged())
        {
#if UNITY_EDITOR
            LogMenuVoiceEvaluation(
                predictedLabel,
                stats,
                volume,
                meanAbs,
                rms,
                false,
                menuVoiceTestLastDiscardReason,
                true);
#endif
            return;
        }

        Debug.Log($"[MenuVoiceTrace] Scores {BuildScoreLog(readable)}");
        Debug.Log($"[MenuVoiceTrace] predictedIndex={predictedIndex}, predictedLabel={predictedLabel}, volume={volume:F8}");
        Debug.Log(
            $"[MenuVoiceDebug] " +
            $"volume={volume:F8} " +
            $"predicted={predictedLabel} " +
            $"top1={GetLabelOrUnknown(stats.Top1Index)} score={stats.Top1Score:F4} " +
            $"top2={GetLabelOrUnknown(stats.Top2Index)} score={stats.Top2Score:F4} " +
            $"margin={stats.Top2Margin:F4} " +
            $"none={stats.NoneScore:F4} " +
            $"time={stats.TimeScore:F4}"
        );
        Debug.Log(BuildMenuAllScoresLog(readable, predictedLabel, stats, volume));

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
            ResetPendingCommand();
            Debug.Log(
                $"[MenuVoiceReject] " +
                $"label={predictedLabel} " +
                $"reason=NoneIgnored " +
                $"volume={volume:F8} " +
                $"top1={GetLabelOrUnknown(stats.Top1Index)} score={stats.Top1Score:F4} " +
                $"top2={GetLabelOrUnknown(stats.Top2Index)} score={stats.Top2Score:F4} " +
                $"top2Margin={stats.Top2Margin:F4} " +
                $"none={stats.NoneScore:F4} " +
                $"time={stats.TimeScore:F4}"
            );
            Debug.Log("[MenuVoiceTrace] RunInference return. reason=predictedLabel none and ignoreNone is true");
#if UNITY_EDITOR
            LogMenuVoiceEvaluation(
                predictedLabel,
                stats,
                volume,
                meanAbs,
                rms,
                false,
                "NoneIgnored",
                true);
#endif
            ClearInferenceContext();
            return;
        }

#if UNITY_EDITOR
        menuVoiceTestLastSafetyRejectReason = "";
#endif
        if (useSafetyFilter && !PassSafetyFilter(predictedLabel, stats, volume))
        {
#if UNITY_EDITOR
            LogMenuVoiceEvaluation(
                predictedLabel,
                stats,
                volume,
                meanAbs,
                rms,
                false,
                menuVoiceTestLastSafetyRejectReason,
                true);
#endif
            ClearInferenceContext();
            return;
        }

        bool isCooldown = IsCooldown(predictedLabel);
        Debug.Log($"[MenuVoiceTrace] Cooldown check. command={predictedLabel}, isCooldown={isCooldown}, lastCommand={lastCommand}, elapsed={Time.unscaledTime - lastCommandTime:F3}, commandCooldown={commandCooldown:F3}");
        if (isCooldown)
        {
            Debug.Log(
                $"[MenuVoiceReject] " +
                $"label={predictedLabel} " +
                $"reason=CommandCooldown " +
                $"required={commandCooldown:F3} " +
                $"actual={Time.unscaledTime - lastCommandTime:F3} " +
                $"volume={volume:F8} " +
                $"top1={GetLabelOrUnknown(stats.Top1Index)} score={stats.Top1Score:F4} " +
                $"top2={GetLabelOrUnknown(stats.Top2Index)} score={stats.Top2Score:F4} " +
                $"top2Margin={stats.Top2Margin:F4} " +
                $"none={stats.NoneScore:F4} " +
                $"noneMargin={stats.Top1Score - stats.NoneScore:F4}"
            );
            Debug.Log("[MenuVoiceTrace] RunInference return. reason=commandCooldown");
#if UNITY_EDITOR
            LogMenuVoiceEvaluation(
                predictedLabel,
                stats,
                volume,
                meanAbs,
                rms,
                false,
                "CommandCooldown",
                true);
#endif
            ClearInferenceContext();
            return;
        }

        if (DiscardIfInferenceContextChanged())
        {
#if UNITY_EDITOR
            LogMenuVoiceEvaluation(
                predictedLabel,
                stats,
                volume,
                meanAbs,
                rms,
                false,
                menuVoiceTestLastDiscardReason,
                true);
#endif
            return;
        }

        lastCommand = predictedLabel;
        lastCommandTime = Time.unscaledTime;

        if (menuVoiceCommandAction != null)
        {
            Debug.Log(
                $"[MenuVoiceAccept] " +
                $"label={predictedLabel} " +
                $"volume={volume:F8} " +
                $"top1={GetLabelOrUnknown(stats.Top1Index)} score={stats.Top1Score:F4} " +
                $"top2={GetLabelOrUnknown(stats.Top2Index)} score={stats.Top2Score:F4} " +
                $"top2Margin={stats.Top2Margin:F4} " +
                $"none={stats.NoneScore:F4} " +
                $"noneMargin={stats.Top1Score - stats.NoneScore:F4} " +
                $"time={stats.TimeScore:F4} " +
                $"consecutive={GetAcceptedConsecutiveText(predictedLabel)}"
            );
#if UNITY_EDITOR
            LogMenuVoiceEvaluation(
                predictedLabel,
                stats,
                volume,
                meanAbs,
                rms,
                true,
                "None",
                true);
#endif
            Debug.Log($"[MenuVoiceTrace] MenuVoiceCommandAction.ExecuteCommand before. command={predictedLabel}, volume={volume:F8}, target={menuVoiceCommandAction.name}, scene={menuVoiceCommandAction.gameObject.scene.name}, activeSelf={menuVoiceCommandAction.gameObject.activeSelf}, activeInHierarchy={menuVoiceCommandAction.gameObject.activeInHierarchy}, enabled={menuVoiceCommandAction.enabled}");
            menuVoiceCommandAction.ExecuteCommand(predictedLabel, volume);
            Debug.Log("[MenuVoiceTrace] MenuVoiceCommandAction.ExecuteCommand after");
        }
        else
        {
            Debug.LogWarning("[MenuVoiceTrace] MenuVoiceCommandAction.ExecuteCommand skipped. menuVoiceCommandAction is null");
#if UNITY_EDITOR
            LogMenuVoiceEvaluation(
                predictedLabel,
                stats,
                volume,
                meanAbs,
                rms,
                false,
                "MenuVoiceCommandActionMissing",
                true);
#endif
        }

        ClearInferenceContext();
    }

    private void CaptureInferenceContext()
    {
        inferenceStartRunnerGeneration = inferenceSessionGeneration;
        inferenceStartMicrophoneGeneration = microphoneInput != null
            ? microphoneInput.CurrentGeneration
            : -1;
    }

    private bool DiscardIfInferenceContextChanged()
    {
        int currentMicrophoneGeneration = microphoneInput != null
            ? microphoneInput.CurrentGeneration
            : -1;
        string reason = null;

        if (!inferenceSessionValid || inferenceStartRunnerGeneration != inferenceSessionGeneration)
        {
            reason = "RunnerGenerationChanged";
        }
        else if (inferenceStartMicrophoneGeneration != currentMicrophoneGeneration)
        {
            reason = "MicrophoneGenerationChanged";
        }

        if (reason == null)
        {
            return false;
        }

#if UNITY_EDITOR
        menuVoiceTestLastDiscardReason = reason;
#endif

        Debug.LogWarning(
            $"[VoiceInferenceDiscard] runner=Menu " +
            $"reason={reason} " +
            $"startRunnerGeneration={inferenceStartRunnerGeneration} " +
            $"currentRunnerGeneration={inferenceSessionGeneration} " +
            $"startMicGeneration={inferenceStartMicrophoneGeneration} " +
            $"currentMicGeneration={currentMicrophoneGeneration}"
        );
        ResetPendingCommand();
        ClearInferenceContext();
        return true;
    }

    private void ClearInferenceContext()
    {
        inferenceStartRunnerGeneration = -1;
        inferenceStartMicrophoneGeneration = -1;
    }

    private void ResetTransientInferenceState()
    {
        timer = 0f;
        lastCommandTime = -999f;
        lastCommand = "";
        pendingCommand = "";
        pendingCommandCount = 0;
        acceptedConsecutiveText = "1/1";
        ClearInferenceContext();
    }

    private bool PassSafetyFilter(string command, PredictionStats stats, float volume)
    {
        if (stats.Top1Score < minimumTopScore)
        {
#if UNITY_EDITOR
            menuVoiceTestLastSafetyRejectReason = "MinimumTopScore";
#endif
            ResetPendingCommand();
            Debug.Log(BuildMenuRejectLog(command, "MinimumTopScore", minimumTopScore, stats.Top1Score, stats, volume));
            return false;
        }

        if (stats.Top2Margin < minimumTop2Margin)
        {
#if UNITY_EDITOR
            menuVoiceTestLastSafetyRejectReason = "MinimumTop2Margin";
#endif
            ResetPendingCommand();
            Debug.Log(BuildMenuRejectLog(command, "MinimumTop2Margin", minimumTop2Margin, stats.Top2Margin, stats, volume));
            return false;
        }

        if (command == "time")
        {
            float timeNoneScoreMargin = stats.TimeScore - stats.NoneScore;
            if (timeNoneScoreMargin < timeNoneMargin)
            {
#if UNITY_EDITOR
                menuVoiceTestLastSafetyRejectReason = "TimeNoneMargin";
#endif
                ResetPendingCommand();
                Debug.Log(BuildMenuRejectLog(command, "TimeNoneMargin", timeNoneMargin, timeNoneScoreMargin, stats, volume));
                return false;
            }

            if (stats.Top2Margin < timeTop2Margin)
            {
#if UNITY_EDITOR
                menuVoiceTestLastSafetyRejectReason = "TimeTop2Margin";
#endif
                ResetPendingCommand();
                Debug.Log(BuildMenuRejectLog(command, "TimeTop2Margin", timeTop2Margin, stats.Top2Margin, stats, volume));
                return false;
            }

            int requiredCount = Mathf.Max(1, timeRequiredConsecutive);
            if (pendingCommand == command)
            {
                pendingCommandCount++;
            }
            else
            {
                pendingCommand = command;
                pendingCommandCount = 1;
            }

            Debug.Log($"[MenuVoiceDebug] time consecutive count={pendingCommandCount}, required={requiredCount}");
            if (pendingCommandCount < requiredCount)
            {
#if UNITY_EDITOR
                menuVoiceTestLastSafetyRejectReason = "TimeConsecutive";
#endif
                Debug.Log(BuildMenuRejectLog(command, "ConsecutiveCount", requiredCount, pendingCommandCount, stats, volume));
                return false;
            }

            acceptedConsecutiveText = $"{pendingCommandCount}/{requiredCount}";
            ResetPendingCommand();
            return true;
        }

        acceptedConsecutiveText = "1/1";
        ResetPendingCommand();
        return true;
    }

    private void ResetPendingCommand()
    {
        pendingCommand = "";
        pendingCommandCount = 0;
    }

    private string GetAcceptedConsecutiveText(string command)
    {
        if (command == "time")
        {
            return acceptedConsecutiveText;
        }

        return "1/1";
    }

    private string BuildMenuRejectLog(string label, string reason, float required, float actual, PredictionStats stats, float volume)
    {
        return
            $"[MenuVoiceReject] " +
            $"label={label} " +
            $"reason={reason} " +
            $"required={required:F4} " +
            $"actual={actual:F4} " +
            $"volume={volume:F8} " +
            $"top1={GetLabelOrUnknown(stats.Top1Index)} score={stats.Top1Score:F4} " +
            $"top2={GetLabelOrUnknown(stats.Top2Index)} score={stats.Top2Score:F4} " +
            $"top2Margin={stats.Top2Margin:F4} " +
            $"none={stats.NoneScore:F4} " +
            $"noneMargin={stats.Top1Score - stats.NoneScore:F4} " +
            $"time={stats.TimeScore:F4}";
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

    private PredictionStats GetPredictionStats(Tensor<float> tensor)
    {
        PredictionStats stats = new PredictionStats
        {
            Top1Index = -1,
            Top2Index = -1,
            Top1Score = float.NegativeInfinity,
            Top2Score = float.NegativeInfinity,
            NoneScore = GetScoreForLabel(tensor, "none"),
            TimeScore = GetScoreForLabel(tensor, "time")
        };

        int count = Mathf.Min(labels.Length, tensor.shape.length);
        for (int i = 0; i < count; i++)
        {
            float score = tensor[i];
            if (score > stats.Top1Score)
            {
                stats.Top2Score = stats.Top1Score;
                stats.Top2Index = stats.Top1Index;
                stats.Top1Score = score;
                stats.Top1Index = i;
            }
            else if (score > stats.Top2Score)
            {
                stats.Top2Score = score;
                stats.Top2Index = i;
            }
        }

        stats.Top2Margin = stats.Top1Score - stats.Top2Score;
        return stats;
    }

    private float GetScoreForLabel(Tensor<float> tensor, string label)
    {
        int index = GetLabelIndex(label);
        if (index < 0 || index >= tensor.shape.length)
        {
            return float.NegativeInfinity;
        }

        return tensor[index];
    }

    private int GetLabelIndex(string label)
    {
        for (int i = 0; i < labels.Length; i++)
        {
            if (labels[i].Trim().ToLowerInvariant() == label)
            {
                return i;
            }
        }

        return -1;
    }

    private string GetLabelOrUnknown(int index)
    {
        if (index < 0 || index >= labels.Length)
        {
            return "unknown";
        }

        return labels[index].Trim().ToLowerInvariant();
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

    private string BuildMenuAllScoresLog(Tensor<float> tensor, string predictedLabel, PredictionStats stats, float volume)
    {
        return
            $"[MenuAllScores] " +
            $"volume={volume:F8} " +
            $"predicted={predictedLabel} " +
            $"ue={GetScoreForLabel(tensor, "ue"):F4} " +
            $"sita={GetScoreForLabel(tensor, "sita"):F4} " +
            $"hidari={GetScoreForLabel(tensor, "hidari"):F4} " +
            $"migi={GetScoreForLabel(tensor, "migi"):F4} " +
            $"kettei={GetScoreForLabel(tensor, "kettei"):F4} " +
            $"time={GetScoreForLabel(tensor, "time"):F4} " +
            $"none={GetScoreForLabel(tensor, "none"):F4} " +
            $"top1={GetLabelOrUnknown(stats.Top1Index)} " +
            $"top1Score={stats.Top1Score:F4} " +
            $"top2={GetLabelOrUnknown(stats.Top2Index)} " +
            $"top2Score={stats.Top2Score:F4} " +
            $"top2Margin={stats.Top2Margin:F4}";
    }

#if UNITY_EDITOR
    private void UpdateExpectedLabelTestMarker()
    {
        if (menuVoiceTestMarkerActive && Time.unscaledTime >= menuVoiceTestMarkerEndTime)
        {
            EndExpectedLabelTestMarker("Timeout");
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        string expectedLabel = null;
        if (keyboard[Key.F1].wasPressedThisFrame)
        {
            expectedLabel = "ue";
        }
        else if (keyboard[Key.F2].wasPressedThisFrame)
        {
            expectedLabel = "sita";
        }
        else if (keyboard[Key.F3].wasPressedThisFrame)
        {
            expectedLabel = "hidari";
        }
        else if (keyboard[Key.F4].wasPressedThisFrame)
        {
            expectedLabel = "migi";
        }
        else if (keyboard[Key.F5].wasPressedThisFrame)
        {
            expectedLabel = "kettei";
        }
        else if (keyboard[Key.F6].wasPressedThisFrame)
        {
            expectedLabel = "time";
        }
        else if (keyboard[Key.F7].wasPressedThisFrame)
        {
            expectedLabel = "none";
        }

        if (expectedLabel != null)
        {
            StartExpectedLabelTestMarker(expectedLabel);
        }
    }

    private void StartExpectedLabelTestMarker(string expectedLabel)
    {
        if (menuVoiceTestMarkerActive)
        {
            EndExpectedLabelTestMarker("Replaced");
        }

        menuVoiceTestMarkerId++;
        menuVoiceTestExpectedLabel = expectedLabel;
        menuVoiceTestMarkerEndTime = Time.unscaledTime + ExpectedLabelMarkerDuration;
        menuVoiceTestMarkerActive = true;

        Debug.Log(
            $"[MenuVoiceTestMarker] " +
            $"markerId={menuVoiceTestMarkerId} " +
            $"expected={menuVoiceTestExpectedLabel} " +
            $"action=Start"
        );
    }

    private void EndExpectedLabelTestMarker(string reason)
    {
        if (!menuVoiceTestMarkerActive)
        {
            return;
        }

        Debug.Log(
            $"[MenuVoiceTestMarker] " +
            $"markerId={menuVoiceTestMarkerId} " +
            $"expected={menuVoiceTestExpectedLabel} " +
            $"action=End " +
            $"reason={reason}"
        );

        menuVoiceTestMarkerActive = false;
        menuVoiceTestExpectedLabel = "";
        menuVoiceTestMarkerEndTime = 0f;
    }

    private void LogMenuVoiceEvaluation(
        string predictedLabel,
        PredictionStats stats,
        float maxAbs,
        float meanAbs,
        float rms,
        bool accepted,
        string rejectReason,
        bool hasScores)
    {
        if (!menuVoiceTestMarkerActive)
        {
            return;
        }

        string top1Label = hasScores ? GetLabelOrUnknown(stats.Top1Index) : "not_run";
        string top2Label = hasScores ? GetLabelOrUnknown(stats.Top2Index) : "not_run";
        float top1Score = hasScores ? stats.Top1Score : float.NaN;
        float top2Score = hasScores ? stats.Top2Score : float.NaN;
        float top2Margin = hasScores ? stats.Top2Margin : float.NaN;
        float noneScore = hasScores ? stats.NoneScore : float.NaN;
        float noneMargin = hasScores ? stats.Top1Score - stats.NoneScore : float.NaN;

        Debug.Log(
            $"[MenuVoiceEvaluation] " +
            $"markerId={menuVoiceTestMarkerId} " +
            $"expected={menuVoiceTestExpectedLabel} " +
            $"predicted={predictedLabel} " +
            $"maxAbs={maxAbs:F8} " +
            $"meanAbs={meanAbs:F8} " +
            $"RMS={rms:F8} " +
            $"top1={top1Label} " +
            $"top1Score={top1Score:F4} " +
            $"top2={top2Label} " +
            $"top2Score={top2Score:F4} " +
            $"top2Margin={top2Margin:F4} " +
            $"noneScore={noneScore:F4} " +
            $"noneMargin={noneMargin:F4} " +
            $"accepted={accepted} " +
            $"rejectReason={rejectReason}"
        );
    }

    private static void GetSampleStatistics(
        float[] samples,
        out float meanAbs,
        out float rms)
    {
        double absoluteSum = 0.0;
        double squareSum = 0.0;
        for (int index = 0; index < samples.Length; index++)
        {
            float sample = samples[index];
            absoluteSum += Mathf.Abs(sample);
            squareSum += sample * sample;
        }

        meanAbs = samples.Length > 0
            ? (float)(absoluteSum / samples.Length)
            : 0f;
        rms = samples.Length > 0
            ? (float)System.Math.Sqrt(squareSum / samples.Length)
            : 0f;
    }
#endif

    private void LogRunnerLifecycle(string action)
    {
        Debug.Log(
            $"[VoiceRunnerLifecycle] runner=Menu " +
            $"object={gameObject.name} " +
            $"action={action} " +
            $"runnerGeneration={inferenceSessionGeneration} " +
            $"micGeneration={(microphoneInput != null ? microphoneInput.CurrentGeneration : -1)} " +
            $"workerValid={worker != null}"
        );
    }

    private void DisposeInferenceResources()
    {
        worker?.Dispose();
        worker = null;
        runtimeModel = null;
        LogRunnerLifecycle("Dispose");
    }

    private void OnDisable()
    {
#if UNITY_EDITOR
        EndExpectedLabelTestMarker("RunnerDisabled");
#endif
        inferenceSessionValid = false;
        LogRunnerLifecycle("Disable");
        ResetTransientInferenceState();
        DisposeInferenceResources();
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
