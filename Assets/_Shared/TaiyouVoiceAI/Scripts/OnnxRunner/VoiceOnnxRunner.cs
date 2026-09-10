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
            state = VoiceProcessState.Idle;
            return;
        }

        cachedVolume = GetMaxAbs(cachedSamples);
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
            Debug.Log("[VoiceDebug] CheckVolumeStep stopped. reason=volume below threshold");
            state = VoiceProcessState.Idle;
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
            new Tensor<float>(new TensorShape(1, 1, 20, 32));

        for (int m = 0; m < 20; m++)
        {
            for (int t = 0; t < 32; t++)
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
            state = VoiceProcessState.Idle;
            return;
        }

        cachedOutput = output.ReadbackAndClone() as Tensor<float>;
        Debug.Log($"[VoiceTrace] 14. ReadbackAndClone success = {cachedOutput != null}");
        Debug.Log($"[VoiceDebug] RunOnnxStep readback. cachedOutputNull={cachedOutput == null}");

        if (cachedOutput == null)
        {
            Debug.Log("[VoiceDebug] RunOnnxStep stopped. reason=readback null");
            state = VoiceProcessState.Idle;
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

        // none の場合は何もしない
        if (cachedLabel == "none")
        {
            Debug.Log("[ActionTrace] HandleResultStep return. reason=predictedLabel is none");
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
            Debug.Log($"[ActionTrace] ExecuteCommand before. target={voiceCommandAction.name}, scene={voiceCommandAction.gameObject.scene.name}, activeSelf={voiceCommandAction.gameObject.activeSelf}, activeInHierarchy={voiceCommandAction.gameObject.activeInHierarchy}, enabled={voiceCommandAction.enabled}");
            voiceCommandAction.ExecuteCommand(cachedLabel, cachedVolume);
            Debug.Log("[ActionTrace] ExecuteCommand after");
        }
        else
        {
            Debug.LogWarning("[ActionTrace] ExecuteCommand skipped. voiceCommandAction is null");
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

    private void OnDisable()
    {
        worker?.Dispose();
    }
}
