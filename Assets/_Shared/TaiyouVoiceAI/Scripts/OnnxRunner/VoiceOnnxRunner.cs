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
    [SerializeField] private float muscleNoneMargin = 2.0f;
    [SerializeField] private float powerNoneMargin = 2.0f;
    [SerializeField] private float yaaNoneMargin = 2.0f;
    [SerializeField] private float minimumTopScore = 1.0f;
    [SerializeField] private float minimumTop2Margin = 1.2f;

    private Worker worker;
    private Model runtimeModel;
    private float timer = 0f;
    private int inferenceSessionGeneration;
    private bool inferenceSessionValid;
    private int inferenceStartRunnerGeneration = -1;
    private int inferenceStartMicrophoneGeneration = -1;

#if UNITY_EDITOR
    private const float ExpectedLabelMarkerDurationSeconds = 2.5f;
    private string activeExpectedLabel;
    private float activeExpectedLabelEndTime;
    private int activeExpectedMarkerId;
    private int nextExpectedMarkerId;
    private string inferenceExpectedLabel;
    private int inferenceExpectedMarkerId;
#endif

    // Python側の labels 順に合わせる
    private readonly string[] labels = { "muscle", "none", "power", "yaa" };

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
        inferenceSessionGeneration++;
        inferenceSessionValid = true;

        Debug.Log("ONNXモデル読み込み成功");
        LogRunnerLifecycle("Initialize");
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
    private float cachedMeanAbs;
    private float cachedRms;
    private string safetyFilterRejectReason;

    private struct PredictionStats
    {
        public int Top1Index;
        public int Top2Index;
        public float Top1Score;
        public float Top2Score;
        public float Top2Margin;
        public float NoneScore;
    }

    private void Update()
    {
#if UNITY_EDITOR
        UpdateExpectedLabelTestMarker();
#endif

        if (worker == null) return;

        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[VoiceDebug] Update running. state={state}, timer={timer:F3}, interval={inferenceInterval}");
        }


        if (state == VoiceProcessState.Idle)
        {
            Debug.Log(
                $"[GameVoiceTrace] Update " +
                $"state={state}, " +
                $"timer={timer:F3}, " +
                $"deltaTime={Time.deltaTime:F6}, " +
                $"unscaledDeltaTime={Time.unscaledDeltaTime:F6}, " +
                $"timeScale={Time.timeScale:F2}"
            );

            timer += Time.deltaTime;

            if (timer >= inferenceInterval)
            {
                Debug.Log(
                    $"[GameVoiceTrace] Idle -> CheckVolume " +
                    $"timer={timer:F3}, interval={inferenceInterval:F3}"
                );

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
        Debug.Log("[VoiceTrace] 1. CheckVolumeStep entered");
        Debug.Log("[VoiceDebug] CheckVolumeStep reached.");

        Debug.Log("[Voice] CheckVolumeStep");
        Debug.Log($"[Voice] samples={(cachedSamples == null ? -1 : cachedSamples.Length)} volume={cachedVolume}");
        Debug.Log("[Voice] MakeFeatureStep start");
        Debug.Log("[Voice] MakeFeatureStep end");
        Debug.Log("[Voice] RunOnnxStep start");
        Debug.Log($"[Voice] readback null? {cachedOutput == null}");
        Debug.Log($"[Voice] predicted={cachedLabel}");

        Debug.Log($"[Voice] CheckVolume samples={(cachedSamples == null ? -1 : cachedSamples.Length)} recording={microphoneInput.IsRecording()}");

        cachedSamples = microphoneInput.GetLatestSamples();
        Debug.Log($"[VoiceTrace] 2. cachedSamples.Length={(cachedSamples == null ? -1 : cachedSamples.Length)}");

        if (cachedSamples == null || cachedSamples.Length == 0)
        {
            Debug.Log($"[VoiceDebug] CheckVolumeStep stopped. samples={(cachedSamples == null ? -1 : cachedSamples.Length)}");
            ResetInferenceCycleState();
            return;
        }

        CaptureInferenceContext();

#if UNITY_EDITOR
        CaptureExpectedLabelForInference();
#endif

        cachedVolume = GetMaxAbs(cachedSamples);
        CalculateWindowStats(cachedSamples, out cachedMeanAbs, out cachedRms);
        bool isBelowThreshold = cachedVolume < minVolumeThreshold;
        Debug.Log(
            $"[GameVoiceTrace] volume={cachedVolume:F8}, " +
            $"threshold={minVolumeThreshold:F8}, " +
            $"belowThreshold={cachedVolume < minVolumeThreshold}"
        );
        Debug.Log($"[VoiceTrace] 3. cachedVolume={cachedVolume:F8}");
        Debug.Log($"[VoiceTrace] 4. minVolumeThreshold={minVolumeThreshold:F8}");
        Debug.Log($"[VoiceTrace] 5. cachedVolume < minVolumeThreshold = {isBelowThreshold}");
        Debug.Log($"[VoiceDebug] CheckVolumeStep samples={cachedSamples.Length}, volume={cachedVolume:F6}, minVolumeThreshold={minVolumeThreshold:F6}, pass={cachedVolume >= minVolumeThreshold}");

        if (isBelowThreshold)
        {
            Debug.Log($"[GameVoiceReject] label=unknown reason=VolumeThreshold required={minVolumeThreshold:F8} actual={cachedVolume:F8}");
#if UNITY_EDITOR
            LogGameVoiceEvaluationWithoutPrediction("VolumeThreshold");
#endif
            Debug.Log("[VoiceDebug] CheckVolumeStep stopped. reason=volume below threshold");
            ResetInferenceCycleState();
            return;
        }

        state = VoiceProcessState.MakeFeature;


    }

    private void MakeFeatureStep()
    {
        Debug.Log("[VoiceTrace] 6. MakeFeatureStep entered");
        Debug.Log($"[VoiceDebug] MakeFeatureStep reached. cachedSamplesLength={(cachedSamples == null ? -1 : cachedSamples.Length)}");

        Debug.Log("[Voice] CheckVolumeStep");
        Debug.Log($"[Voice] samples={(cachedSamples == null ? -1 : cachedSamples.Length)} volume={cachedVolume}");
        Debug.Log("[Voice] MakeFeatureStep start");
        Debug.Log("[Voice] MakeFeatureStep end");
        Debug.Log("[Voice] RunOnnxStep start");
        Debug.Log($"[Voice] readback null? {cachedOutput == null}");
        Debug.Log($"[Voice] predicted={cachedLabel}");

        Debug.Log("[VoiceTrace] 7. ExtractDummyMfcc start");
        cachedMfcc = featureExtractor.ExtractDummyMfcc(cachedSamples);
        Debug.Log("[VoiceTrace] 8. ExtractDummyMfcc end");
        Debug.Log($"[VoiceTrace] 9. MFCC size rows={(cachedMfcc == null ? -1 : cachedMfcc.GetLength(0))}, cols={(cachedMfcc == null ? -1 : cachedMfcc.GetLength(1))}");
        Debug.Log($"[VoiceTrace] 10. MFCC has NaN/Infinity = {HasInvalidMfccValue(cachedMfcc)}");
        Debug.Log($"[VoiceDebug] MakeFeatureStep completed. mfccNull={cachedMfcc == null}, rows={(cachedMfcc == null ? -1 : cachedMfcc.GetLength(0))}, cols={(cachedMfcc == null ? -1 : cachedMfcc.GetLength(1))}");
        state = VoiceProcessState.RunOnnx;
    }

    private void RunOnnxStep()
    {
        Debug.Log("[VoiceTrace] 11. RunOnnxStep entered");
        Debug.Log("[VoiceDebug] RunOnnxStep reached.");

        Debug.Log("[Voice] CheckVolumeStep");
        Debug.Log($"[Voice] samples={(cachedSamples == null ? -1 : cachedSamples.Length)} volume={cachedVolume}");
        Debug.Log("[Voice] MakeFeatureStep start");
        Debug.Log("[Voice] MakeFeatureStep end");
        Debug.Log("[Voice] RunOnnxStep start");
        Debug.Log($"[Voice] readback null? {cachedOutput == null}");
        Debug.Log($"[Voice] predicted={cachedLabel}");
        using Tensor<float> inputTensor =
            new Tensor<float>(new TensorShape(
                1,
                1,
                MfccFeatureExtractor.MfccCount,
                MfccFeatureExtractor.TargetWidth));

        for (int m = 0; m < MfccFeatureExtractor.MfccCount; m++)
        {
            for (int t = 0; t < MfccFeatureExtractor.TargetWidth; t++)
            {
                inputTensor[0, 0, m, t] = cachedMfcc[m, t];
            }
        }

        Debug.Log("[VoiceTrace] 12. worker.Schedule before");
        worker.Schedule(inputTensor);
        Debug.Log("[VoiceTrace] 13. worker.Schedule after");

        Tensor<float> output = worker.PeekOutput() as Tensor<float>;
        if (output == null)
        {
            Debug.Log("[VoiceDebug] RunOnnxStep stopped. reason=output null");
            ResetInferenceCycleState();
            return;
        }

        cachedOutput = output.ReadbackAndClone() as Tensor<float>;
        Debug.Log($"[VoiceTrace] 14. ReadbackAndClone success = {cachedOutput != null}");
        Debug.Log($"[VoiceDebug] RunOnnxStep readback. cachedOutputNull={cachedOutput == null}");

        if (cachedOutput == null)
        {
            Debug.Log("[VoiceDebug] RunOnnxStep stopped. reason=readback null");
            ResetInferenceCycleState();
            return;
        }

        int predictedIndex = ArgMax(cachedOutput);
        Debug.Log($"[VoiceTrace] 15. predictedIndex={predictedIndex}");
        cachedLabel = labels[predictedIndex];
        Debug.Log($"[VoiceTrace] 16. predictedLabel={cachedLabel}");
        Debug.Log($"[VoiceDebug] RunOnnxStep predictedLabel={cachedLabel}, predictedIndex={predictedIndex}");

        state = VoiceProcessState.HandleResult;
    }

    private void HandleResultStep()
    {
        if (DiscardIfInferenceContextChanged())
        {
            return;
        }

        Debug.Log("[VoiceTrace] 17. HandleResultStep entered");
        Debug.Log($"[VoiceDebug] HandleResultStep reached. predictedLabel={cachedLabel}, volume={cachedVolume:F6}");
        Debug.Log($"[ActionTrace] HandleResultStep label={cachedLabel}, volume={cachedVolume:F8}, voiceCommandActionNull={voiceCommandAction == null}");

        if (logPrediction)
        {
            Debug.Log($"[AI] result={cachedLabel} / volume={cachedVolume:F4}");
        }

        if (logScores && cachedOutput != null)
        {
            Debug.Log(BuildScoreLog(cachedOutput));
        }

        if (cachedOutput != null)
        {
            PredictionStats stats = GetPredictionStats(cachedOutput);
            Debug.Log(
                $"[GameVoiceDebug] " +
                $"volume={cachedVolume:F8} " +
                $"predicted={cachedLabel} " +
                $"top1={GetLabelOrUnknown(stats.Top1Index)} score={stats.Top1Score:F4} " +
                $"top2={GetLabelOrUnknown(stats.Top2Index)} score={stats.Top2Score:F4} " +
                $"margin={stats.Top2Margin:F4} " +
                $"none={stats.NoneScore:F4} " +
                $"noneMargin={stats.Top1Score - stats.NoneScore:F4}"
            );

            if (useSafetyFilter && !PassSafetyFilter(cachedLabel, stats, cachedVolume))
            {
#if UNITY_EDITOR
                LogGameVoiceEvaluation(stats, false, safetyFilterRejectReason ?? "SafetyFilter");
#endif
                ResetInferenceCycleState();
                return;
            }
        }

        // none の場合は何もしない
        if (cachedLabel == "none")
        {
            if (cachedOutput != null)
            {
                PredictionStats stats = GetPredictionStats(cachedOutput);
#if UNITY_EDITOR
                LogGameVoiceEvaluation(stats, false, "NoneIgnored");
#endif
                Debug.Log(
                    $"[GameVoiceReject] " +
                    $"label={cachedLabel} " +
                    $"reason=NoneIgnored " +
                    $"volume={cachedVolume:F8} " +
                    $"top1={GetLabelOrUnknown(stats.Top1Index)} score={stats.Top1Score:F4} " +
                    $"top2={GetLabelOrUnknown(stats.Top2Index)} score={stats.Top2Score:F4} " +
                    $"top2Margin={stats.Top2Margin:F4} " +
                    $"none={stats.NoneScore:F4} " +
                    $"noneMargin={stats.Top1Score - stats.NoneScore:F4}"
                );
            }

            Debug.Log("[ActionTrace] HandleResultStep return. reason=predictedLabel is none");
            ResetInferenceCycleState();
            return;
        }

        // 認識結果を実行
        if (DiscardIfInferenceContextChanged())
        {
            return;
        }

        if (voiceCommandAction != null)
        {
            if (cachedOutput != null)
            {
                PredictionStats stats = GetPredictionStats(cachedOutput);
#if UNITY_EDITOR
                LogGameVoiceEvaluation(stats, true, "None");
#endif
                Debug.Log(
                    $"[GameVoiceAccept] " +
                    $"label={cachedLabel} " +
                    $"volume={cachedVolume:F8} " +
                    $"top1={GetLabelOrUnknown(stats.Top1Index)} score={stats.Top1Score:F4} " +
                    $"top2={GetLabelOrUnknown(stats.Top2Index)} score={stats.Top2Score:F4} " +
                    $"top2Margin={stats.Top2Margin:F4} " +
                    $"none={stats.NoneScore:F4} " +
                    $"noneMargin={stats.Top1Score - stats.NoneScore:F4}"
                );
            }

            Debug.Log($"[ActionTrace] ExecuteCommand before. target={voiceCommandAction.name}, scene={voiceCommandAction.gameObject.scene.name}, activeSelf={voiceCommandAction.gameObject.activeSelf}, activeInHierarchy={voiceCommandAction.gameObject.activeInHierarchy}, enabled={voiceCommandAction.enabled}");
            voiceCommandAction.ExecuteCommand(cachedLabel, cachedVolume);
            Debug.Log("[ActionTrace] ExecuteCommand after");
        }
        else
        {
#if UNITY_EDITOR
            if (cachedOutput != null)
            {
                LogGameVoiceEvaluation(
                    GetPredictionStats(cachedOutput),
                    false,
                    "VoiceCommandActionUnavailable");
            }
#endif
            Debug.LogWarning("[ActionTrace] ExecuteCommand skipped. voiceCommandAction is null");
        }

        ResetInferenceCycleState();
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

        LogInferenceDiscard(
            reason,
            inferenceStartRunnerGeneration,
            inferenceSessionGeneration,
            inferenceStartMicrophoneGeneration,
            currentMicrophoneGeneration
        );
        ResetInferenceCycleState();
        return true;
    }

    private void ResetInferenceCycleState()
    {
        cachedOutput?.Dispose();
        cachedOutput = null;
        cachedSamples = null;
        cachedVolume = 0f;
        cachedMfcc = null;
        cachedLabel = null;
        cachedMeanAbs = 0f;
        cachedRms = 0f;
        safetyFilterRejectReason = null;
        inferenceStartRunnerGeneration = -1;
        inferenceStartMicrophoneGeneration = -1;
#if UNITY_EDITOR
        inferenceExpectedLabel = null;
        inferenceExpectedMarkerId = 0;
#endif
        state = VoiceProcessState.Idle;
    }

    private void ResetTransientInferenceState()
    {
        timer = 0f;
        ResetInferenceCycleState();
    }

    private void LogInferenceDiscard(
        string reason,
        int startRunnerGeneration,
        int currentRunnerGeneration,
        int startMicrophoneGeneration,
        int currentMicrophoneGeneration)
    {
        Debug.LogWarning(
            $"[VoiceInferenceDiscard] runner=Game " +
            $"reason={reason} " +
            $"startRunnerGeneration={startRunnerGeneration} " +
            $"currentRunnerGeneration={currentRunnerGeneration} " +
            $"startMicGeneration={startMicrophoneGeneration} " +
            $"currentMicGeneration={currentMicrophoneGeneration}"
        );
    }


    private void RunInferenceWithSamples(float[] samples, float volume)
    {
        int startRunnerGeneration = inferenceSessionGeneration;
        int startMicrophoneGeneration = microphoneInput != null
            ? microphoneInput.CurrentGeneration
            : -1;
        float[,] mfcc = featureExtractor.ExtractDummyMfcc(samples);

        using Tensor<float> inputTensor =
            new Tensor<float>(new TensorShape(
                1,
                1,
                MfccFeatureExtractor.MfccCount,
                MfccFeatureExtractor.TargetWidth));

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

        Tensor<float> readable =
            output.ReadbackAndClone() as Tensor<float>;

        if (readable == null)
        {
            return;
        }

        int predictedIndex = ArgMax(readable);
        string predictedLabel = labels[predictedIndex];

        Debug.Log("[AI] " + predictedLabel);

        int currentMicrophoneGeneration = microphoneInput != null
            ? microphoneInput.CurrentGeneration
            : -1;
        if (!inferenceSessionValid || startRunnerGeneration != inferenceSessionGeneration)
        {
            LogInferenceDiscard(
                "RunnerGenerationChanged",
                startRunnerGeneration,
                inferenceSessionGeneration,
                startMicrophoneGeneration,
                currentMicrophoneGeneration
            );
            readable.Dispose();
            return;
        }

        if (startMicrophoneGeneration != currentMicrophoneGeneration)
        {
            LogInferenceDiscard(
                "MicrophoneGenerationChanged",
                startRunnerGeneration,
                inferenceSessionGeneration,
                startMicrophoneGeneration,
                currentMicrophoneGeneration
            );
            readable.Dispose();
            return;
        }

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

    private void CalculateWindowStats(float[] samples, out float meanAbs, out float rms)
    {
        double absoluteSum = 0.0;
        double squareSum = 0.0;

        for (int i = 0; i < samples.Length; i++)
        {
            float sample = samples[i];
            absoluteSum += Mathf.Abs(sample);
            squareSum += sample * sample;
        }

        meanAbs = (float)(absoluteSum / samples.Length);
        rms = Mathf.Sqrt((float)(squareSum / samples.Length));
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

    private bool PassSafetyFilter(string label, PredictionStats stats, float volume)
    {
        safetyFilterRejectReason = null;

        if (label == "none")
        {
            return true;
        }

        if (stats.Top1Score < minimumTopScore)
        {
            safetyFilterRejectReason = "MinimumTopScore";
            Debug.Log(BuildGameRejectLog(label, "MinimumTopScore", minimumTopScore, stats.Top1Score, stats, volume));
            return false;
        }

        if (stats.Top2Margin < minimumTop2Margin)
        {
            safetyFilterRejectReason = "MinimumTop2Margin";
            Debug.Log(BuildGameRejectLog(label, "MinimumTop2Margin", minimumTop2Margin, stats.Top2Margin, stats, volume));
            return false;
        }

        float requiredNoneMargin = GetRequiredNoneMargin(label);
        float noneMargin = stats.Top1Score - stats.NoneScore;
        if (noneMargin < requiredNoneMargin)
        {
            safetyFilterRejectReason = GetNoneMarginReason(label);
            Debug.Log(BuildGameRejectLog(label, safetyFilterRejectReason, requiredNoneMargin, noneMargin, stats, volume));
            return false;
        }

        return true;
    }

#if UNITY_EDITOR
    private void UpdateExpectedLabelTestMarker()
    {
        if (!string.IsNullOrEmpty(activeExpectedLabel) &&
            Time.unscaledTime >= activeExpectedLabelEndTime)
        {
            EndExpectedLabelTestMarker("Timeout");
        }

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        string requestedLabel = null;
        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            requestedLabel = "power";
        }
        else if (keyboard.digit2Key.wasPressedThisFrame)
        {
            requestedLabel = "muscle";
        }
        else if (keyboard.digit3Key.wasPressedThisFrame)
        {
            requestedLabel = "yaa";
        }
        else if (keyboard.digit4Key.wasPressedThisFrame)
        {
            requestedLabel = "none";
        }

        if (requestedLabel != null)
        {
            StartExpectedLabelTestMarker(requestedLabel);
        }
    }

    private void StartExpectedLabelTestMarker(string expectedLabel)
    {
        if (!string.IsNullOrEmpty(activeExpectedLabel))
        {
            EndExpectedLabelTestMarker("Replaced");
        }

        activeExpectedLabel = expectedLabel;
        activeExpectedLabelEndTime = Time.unscaledTime + ExpectedLabelMarkerDurationSeconds;
        activeExpectedMarkerId = ++nextExpectedMarkerId;

        Debug.Log(
            $"[GameVoiceTestMarker] " +
            $"markerId={activeExpectedMarkerId} " +
            $"expected={activeExpectedLabel} " +
            $"action=Start " +
            $"duration={ExpectedLabelMarkerDurationSeconds:F1}");
    }

    private void EndExpectedLabelTestMarker(string reason)
    {
        if (string.IsNullOrEmpty(activeExpectedLabel))
        {
            return;
        }

        Debug.Log(
            $"[GameVoiceTestMarker] " +
            $"markerId={activeExpectedMarkerId} " +
            $"expected={activeExpectedLabel} " +
            $"action=End " +
            $"reason={reason}");

        activeExpectedLabel = null;
        activeExpectedLabelEndTime = 0f;
        activeExpectedMarkerId = 0;
    }

    private void CaptureExpectedLabelForInference()
    {
        inferenceExpectedLabel = activeExpectedLabel;
        inferenceExpectedMarkerId = activeExpectedMarkerId;
    }

    private void LogGameVoiceEvaluation(PredictionStats stats, bool accepted, string rejectReason)
    {
        if (string.IsNullOrEmpty(inferenceExpectedLabel))
        {
            return;
        }

        Debug.Log(
            $"[GameVoiceEvaluation] " +
            $"markerId={inferenceExpectedMarkerId} " +
            $"expected={inferenceExpectedLabel} " +
            $"predicted={cachedLabel} " +
            $"volume={cachedVolume:F8} " +
            $"maxAbs={cachedVolume:F8} " +
            $"meanAbs={cachedMeanAbs:F8} " +
            $"RMS={cachedRms:F8} " +
            $"top1={GetLabelOrUnknown(stats.Top1Index)} " +
            $"top1Score={stats.Top1Score:F4} " +
            $"top2={GetLabelOrUnknown(stats.Top2Index)} " +
            $"top2Score={stats.Top2Score:F4} " +
            $"top2Margin={stats.Top2Margin:F4} " +
            $"none={stats.NoneScore:F4} " +
            $"noneMargin={stats.Top1Score - stats.NoneScore:F4} " +
            $"accepted={accepted} " +
            $"rejectReason={rejectReason}");
    }

    private void LogGameVoiceEvaluationWithoutPrediction(string rejectReason)
    {
        if (string.IsNullOrEmpty(inferenceExpectedLabel))
        {
            return;
        }

        Debug.Log(
            $"[GameVoiceEvaluation] " +
            $"markerId={inferenceExpectedMarkerId} " +
            $"expected={inferenceExpectedLabel} " +
            $"predicted=not_run " +
            $"volume={cachedVolume:F8} " +
            $"maxAbs={cachedVolume:F8} " +
            $"meanAbs={cachedMeanAbs:F8} " +
            $"RMS={cachedRms:F8} " +
            $"top1=not_run top1Score=NaN " +
            $"top2=not_run top2Score=NaN " +
            $"top2Margin=NaN none=NaN noneMargin=NaN " +
            $"accepted=False " +
            $"rejectReason={rejectReason}");
    }
#endif

    private string GetNoneMarginReason(string label)
    {
        switch (label)
        {
            case "muscle":
                return "MuscleNoneMargin";
            case "power":
                return "PowerNoneMargin";
            case "yaa":
                return "YaaNoneMargin";
            default:
                return "NoneMargin";
        }
    }

    private string BuildGameRejectLog(string label, string reason, float required, float actual, PredictionStats stats, float volume)
    {
        return
            $"[GameVoiceReject] " +
            $"label={label} " +
            $"reason={reason} " +
            $"required={required:F4} " +
            $"actual={actual:F4} " +
            $"volume={volume:F8} " +
            $"top1={GetLabelOrUnknown(stats.Top1Index)} score={stats.Top1Score:F4} " +
            $"top2={GetLabelOrUnknown(stats.Top2Index)} score={stats.Top2Score:F4} " +
            $"top2Margin={stats.Top2Margin:F4} " +
            $"none={stats.NoneScore:F4} " +
            $"noneMargin={stats.Top1Score - stats.NoneScore:F4}";
    }

    private float GetRequiredNoneMargin(string label)
    {
        switch (label)
        {
            case "muscle":
                return muscleNoneMargin;
            case "power":
                return powerNoneMargin;
            case "yaa":
                return yaaNoneMargin;
            default:
                return 0f;
        }
    }

    private PredictionStats GetPredictionStats(Tensor<float> tensor)
    {
        PredictionStats stats = new PredictionStats
        {
            Top1Index = -1,
            Top2Index = -1,
            Top1Score = float.NegativeInfinity,
            Top2Score = float.NegativeInfinity,
            NoneScore = GetScoreForLabel(tensor, "none")
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
            if (labels[i] == label)
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

        return labels[index];
    }

    private bool HasInvalidMfccValue(float[,] mfcc)
    {
        if (mfcc == null)
        {
            return true;
        }

        int rows = mfcc.GetLength(0);
        int cols = mfcc.GetLength(1);

        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                float value = mfcc[r, c];
                if (float.IsNaN(value) || float.IsInfinity(value))
                {
                    return true;
                }
            }
        }

        return false;
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

    private void LogRunnerLifecycle(string action)
    {
        Debug.Log(
            $"[VoiceRunnerLifecycle] runner=Game " +
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
}
