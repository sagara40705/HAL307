using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MicrophoneInput : MonoBehaviour
{
    [SerializeField] private int sampleRate = 16000;
    [SerializeField] private int recordLengthSec = 1;

    private AudioClip microphoneClip;
    private string deviceName;
    private int sampleCount;
    private float[] sampleBuffer;
    private SharedRecording sharedRecording;
    private Coroutine recordingCoroutine;
    private bool isUsingSharedRecording;
    private float nextSampleStatsLogTime;
    private float nextMicCompareLogTime;

    private sealed class SharedRecording
    {
        public AudioClip Clip;
        public int UseCount;
    }

    private static readonly Dictionary<string, SharedRecording> SharedRecordings = new();

    private void Start()
    {
        sampleCount = sampleRate * recordLengthSec;
        sampleBuffer = new float[sampleCount];

        recordingCoroutine = StartCoroutine(StartRecordingWithRetry());
    }

    private void OnEnable()
    {
        Debug.Log($"[MicDebug] OnEnable object={gameObject.name}, scene={gameObject.scene.name}, sampleBufferReady={sampleBuffer != null}, isUsingShared={isUsingSharedRecording}");
        LogSharedMicTrace("OnEnable");

        if (sampleBuffer != null && !isUsingSharedRecording)
        {
            recordingCoroutine = StartCoroutine(StartRecordingWithRetry());
        }
    }

    private IEnumerator StartRecordingWithRetry()
    {
        // timeScale = 0 でも進むように Realtime を使う
        yield return new WaitForSecondsRealtime(0.5f);

        for (int i = 0; i < 10; i++)
        {
            Debug.Log($"マイク確認 {i + 1}回目 / マイク数: {Microphone.devices.Length}");

            if (Microphone.devices.Length > 0)
            {
                deviceName = Microphone.devices[0];
                AcquireSharedRecording();
                yield break;
            }

            yield return new WaitForSecondsRealtime(0.5f);
        }

        Debug.LogError("マイクが見つかりません。");
    }

    private void AcquireSharedRecording()
    {
        LogSharedMicTrace("AcquireSharedRecording enter");

        if (isUsingSharedRecording)
        {
            LogSharedMicTrace("AcquireSharedRecording skip already using shared");
            return;
        }

        if (string.IsNullOrEmpty(deviceName))
        {
            LogSharedMicTrace("AcquireSharedRecording skip empty deviceName");
            return;
        }

        if (!SharedRecordings.TryGetValue(deviceName, out sharedRecording) || sharedRecording == null || sharedRecording.Clip == null)
        {
            Debug.Log("使用するマイク: " + deviceName);

            AudioClip clip = Microphone.Start(
                deviceName,
                true,
                recordLengthSec,
                sampleRate
            );

            if (clip == null)
            {
                Debug.LogError("Microphone.Start に失敗しました");
                sharedRecording = null;
                return;
            }

            sharedRecording = new SharedRecording
            {
                Clip = clip,
                UseCount = 0
            };

            SharedRecordings[deviceName] = sharedRecording;
            Debug.Log("マイク録音開始");
            Debug.Log($"[MicDebug] Created shared recording. object={gameObject.name}, deviceName={deviceName}");
            LogSharedMicTrace("AcquireSharedRecording created shared recording");
        }
        else
        {
            Debug.Log($"[MicDebug] Reusing shared recording. object={gameObject.name}, deviceName={deviceName}");
            LogSharedMicTrace("AcquireSharedRecording reusing shared recording");
        }

        sharedRecording.UseCount++;
        microphoneClip = sharedRecording.Clip;
        isUsingSharedRecording = true;
        Debug.Log($"[MicDebug] Joined shared recording. object={gameObject.name}, deviceName={deviceName}, useCount={sharedRecording.UseCount}");
        LogSharedMicTrace("AcquireSharedRecording joined shared recording");
    }

    public bool IsRecording()
    {
        if (string.IsNullOrEmpty(deviceName))
        {
            return false;
        }

        return Microphone.IsRecording(deviceName);
    }

    public void RestartRecordingIfNeeded()
    {
        LogSharedMicTrace("RestartRecordingIfNeeded enter");

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("マイクが見つかりません。");
            LogSharedMicTrace("RestartRecordingIfNeeded no devices");
            return;
        }

        if (string.IsNullOrEmpty(deviceName))
        {
            deviceName = Microphone.devices[0];
            LogSharedMicTrace("RestartRecordingIfNeeded assigned deviceName");
        }

        if (!Microphone.IsRecording(deviceName))
        {
            Debug.LogWarning("マイクが停止していたので再開します。");
            int oldLocalClipId = microphoneClip != null ? microphoneClip.GetInstanceID() : -1;
            int oldSharedClipId = GetSharedClipId();
            int oldUseCount = GetSharedUseCount();
            Debug.LogWarning($"[SharedMicTrace] RestartRecordingIfNeeded recreating. object={gameObject.name}, scene={gameObject.scene.name}, device={deviceName}, oldLocalClipId={oldLocalClipId}, oldSharedClipId={oldSharedClipId}, oldUseCount={oldUseCount}");

            if (isUsingSharedRecording && sharedRecording != null)
            {
                sharedRecording.UseCount = Mathf.Max(0, sharedRecording.UseCount - 1);
                isUsingSharedRecording = false;
            }

            microphoneClip = null;
            sharedRecording = null;
            SharedRecordings.Remove(deviceName);
            AcquireSharedRecording();
            Debug.LogWarning($"[SharedMicTrace] RestartRecordingIfNeeded recreated. object={gameObject.name}, scene={gameObject.scene.name}, device={deviceName}, newLocalClipId={(microphoneClip != null ? microphoneClip.GetInstanceID() : -1)}, newSharedClipId={GetSharedClipId()}, newUseCount={GetSharedUseCount()}");
            LogSharedMicTrace("RestartRecordingIfNeeded after recreate");
        }
    }

    public float[] GetLatestSamples()
    {
        Debug.Log($"[MicDebug] GetLatestSamples called. object={gameObject.name}, deviceName={deviceName}, isUsingShared={isUsingSharedRecording}");
        LogSharedMicTrace("GetLatestSamples enter");

        RestartRecordingIfNeeded();
        LogSharedMicTrace("GetLatestSamples after RestartRecordingIfNeeded");

        if (microphoneClip == null || sampleBuffer == null)
        {
            Debug.Log($"[MicDebug] GetLatestSamples return null. object={gameObject.name}, clipNull={microphoneClip == null}, bufferNull={sampleBuffer == null}");
            LogSharedMicTrace("GetLatestSamples return null clip or buffer");
            return null;
        }

        int micPosition = Microphone.GetPosition(deviceName);
        Debug.Log($"[MicDebug] Microphone.GetPosition. object={gameObject.name}, deviceName={deviceName}, position={micPosition}");
        LogSharedMicTrace("GetLatestSamples after GetPosition");

        if (micPosition <= 0)
        {
            Debug.Log($"[MicDebug] GetLatestSamples return null. object={gameObject.name}, reason=micPosition<=0");
            LogSharedMicTrace("GetLatestSamples return null position");
            return null;
        }

        int startPosition = micPosition - sampleCount;

        if (startPosition < 0)
        {
            startPosition += microphoneClip.samples;
        }

        microphoneClip.GetData(sampleBuffer, startPosition);

        LogMicCompareIfNeeded(sampleBuffer, micPosition, startPosition);
        LogSampleStatsIfNeeded(sampleBuffer);

        Debug.Log($"[MicDebug] GetLatestSamples returned. object={gameObject.name}, samples={sampleBuffer.Length}, startPosition={startPosition}");
        LogSharedMicTrace("GetLatestSamples returned samples");
        return sampleBuffer;
    }

    private void LogSampleStatsIfNeeded(float[] samples)
    {
        if (Time.unscaledTime < nextSampleStatsLogTime)
        {
            return;
        }

        nextSampleStatsLogTime = Time.unscaledTime + 1f;

        if (samples == null || samples.Length == 0)
        {
            Debug.Log($"[MicSampleStats] object={gameObject.name}, scene={gameObject.scene.name}, samples=0");
            return;
        }

        float min = samples[0];
        float max = samples[0];
        float maxAbs = Mathf.Abs(samples[0]);
        double absSum = 0.0;
        double squareSum = 0.0;
        int clippedCount = 0;

        for (int i = 0; i < samples.Length; i++)
        {
            float sample = samples[i];
            float abs = Mathf.Abs(sample);

            if (sample < min)
            {
                min = sample;
            }

            if (sample > max)
            {
                max = sample;
            }

            if (abs > maxAbs)
            {
                maxAbs = abs;
            }

            absSum += abs;
            squareSum += sample * sample;

            if (abs >= 0.99f)
            {
                clippedCount++;
            }
        }

        float meanAbs = (float)(absSum / samples.Length);
        float rms = Mathf.Sqrt((float)(squareSum / samples.Length));

        Debug.Log($"[MicSampleStats] object={gameObject.name}, scene={gameObject.scene.name}, min={min:F6} max={max:F6} maxAbs={maxAbs:F6} meanAbs={meanAbs:F6} RMS={rms:F6} clipped={clippedCount}/{samples.Length}");
    }

    private void LogMicCompareIfNeeded(float[] samples, int currentPosition, int startPosition)
    {
        if (Time.unscaledTime < nextMicCompareLogTime)
        {
            return;
        }

        nextMicCompareLogTime = Time.unscaledTime + 1f;

        if (samples == null || samples.Length == 0)
        {
            Debug.Log($"[MicCompare] object={gameObject.name} scene={gameObject.scene.name} device={deviceName} samples=0");
            return;
        }

        float maxAbs = 0f;
        double absSum = 0.0;
        double squareSum = 0.0;

        for (int i = 0; i < samples.Length; i++)
        {
            float abs = Mathf.Abs(samples[i]);
            if (abs > maxAbs)
            {
                maxAbs = abs;
            }

            absSum += abs;
            squareSum += samples[i] * samples[i];
        }

        float meanAbs = (float)(absSum / samples.Length);
        float rms = Mathf.Sqrt((float)(squareSum / samples.Length));
        int localClipId = microphoneClip != null ? microphoneClip.GetInstanceID() : -1;
        int sharedClipId = GetSharedClipId();
        int clipSamples = microphoneClip != null ? microphoneClip.samples : -1;
        int frequency = microphoneClip != null ? microphoneClip.frequency : -1;
        int channels = microphoneClip != null ? microphoneClip.channels : -1;

        Debug.Log(
            $"[MicCompare] " +
            $"object={gameObject.name} " +
            $"scene={gameObject.scene.name} " +
            $"device={deviceName} " +
            $"localClipId={localClipId} " +
            $"sharedClipId={sharedClipId} " +
            $"sameClip={localClipId != -1 && localClipId == sharedClipId} " +
            $"position={currentPosition} " +
            $"startPosition={startPosition} " +
            $"clipSamples={clipSamples} " +
            $"frequency={frequency} " +
            $"channels={channels} " +
            $"sampleBufferLength={sampleBuffer?.Length ?? -1} " +
            $"maxAbs={maxAbs:F8} " +
            $"meanAbs={meanAbs:F8} " +
            $"RMS={rms:F8} " +
            $"useCount={GetSharedUseCount()} " +
            $"frame={Time.frameCount}"
        );
    }

    public float GetCurrentVolume()
    {
        float[] samples = GetLatestSamples();

        if (samples == null || samples.Length == 0)
        {
            return 0f;
        }

        float max = 0f;

        for (int i = 0; i < samples.Length; i++)
        {
            float abs = Mathf.Abs(samples[i]);

            if (abs > max)
            {
                max = abs;
            }
        }

        return Mathf.Clamp01(max);
    }

    private void OnDisable()
    {
        Debug.LogWarning("MicrophoneInput が Disable されました");
        LogSharedMicTrace("OnDisable before release");

        if (recordingCoroutine != null)
        {
            StopCoroutine(recordingCoroutine);
            recordingCoroutine = null;
        }

        ReleaseSharedRecording();
        LogSharedMicTrace("OnDisable after release");
        Debug.LogWarning($"MicrophoneInput disabled. object={gameObject.name}, scene={gameObject.scene.name}, sceneIsLoaded={gameObject.scene.isLoaded}, activeSelf={gameObject.activeSelf}, activeInHierarchy={gameObject.activeInHierarchy}, frame={Time.frameCount}");
    }

    private void ReleaseSharedRecording()
    {
        LogSharedMicTrace("ReleaseSharedRecording enter");

        if (!isUsingSharedRecording || string.IsNullOrEmpty(deviceName) || sharedRecording == null)
        {
            microphoneClip = null;
            sharedRecording = null;
            isUsingSharedRecording = false;
            LogSharedMicTrace("ReleaseSharedRecording skip not using shared");
            return;
        }

        int beforeUseCount = sharedRecording.UseCount;
        sharedRecording.UseCount = Mathf.Max(0, sharedRecording.UseCount - 1);
        Debug.LogWarning($"[SharedMicTrace] ReleaseSharedRecording decremented. object={gameObject.name}, scene={gameObject.scene.name}, device={deviceName}, beforeUseCount={beforeUseCount}, afterUseCount={sharedRecording.UseCount}, localClipId={(microphoneClip != null ? microphoneClip.GetInstanceID() : -1)}, sharedClipId={(sharedRecording.Clip != null ? sharedRecording.Clip.GetInstanceID() : -1)}");

        if (sharedRecording.UseCount == 0)
        {
            if (Microphone.IsRecording(deviceName))
            {
                Microphone.End(deviceName);
                Debug.LogWarning("マイク録音終了");
                Debug.LogWarning($"[SharedMicTrace] ReleaseSharedRecording Microphone.End called. object={gameObject.name}, scene={gameObject.scene.name}, device={deviceName}");
            }

            SharedRecordings.Remove(deviceName);
            Debug.LogWarning($"[SharedMicTrace] ReleaseSharedRecording removed shared recording. object={gameObject.name}, scene={gameObject.scene.name}, device={deviceName}");
        }

        microphoneClip = null;
        sharedRecording = null;
        isUsingSharedRecording = false;
        LogSharedMicTrace("ReleaseSharedRecording exit");
    }

    private void LogSharedMicTrace(string point)
    {
        bool hasDevice = !string.IsNullOrEmpty(deviceName);
        bool isRecording = hasDevice && Microphone.IsRecording(deviceName);
        int position = hasDevice ? Microphone.GetPosition(deviceName) : -1;
        int localClipId = microphoneClip != null ? microphoneClip.GetInstanceID() : -1;
        int sharedClipId = GetSharedClipId();
        int useCount = GetSharedUseCount();
        bool sameClip = localClipId != -1 && localClipId == sharedClipId;

        Debug.Log(
            $"[SharedMicTrace] point={point} " +
            $"object={gameObject.name} " +
            $"scene={gameObject.scene.name} " +
            $"sceneIsLoaded={gameObject.scene.isLoaded} " +
            $"device={deviceName} " +
            $"isUsingShared={isUsingSharedRecording} " +
            $"isRecording={isRecording} " +
            $"position={position} " +
            $"localClipNull={microphoneClip == null} " +
            $"localClipId={localClipId} " +
            $"sharedClipId={sharedClipId} " +
            $"sameClip={sameClip} " +
            $"useCount={useCount} " +
            $"frame={Time.frameCount}"
        );
    }

    private int GetSharedClipId()
    {
        if (string.IsNullOrEmpty(deviceName))
        {
            return -1;
        }

        if (!SharedRecordings.TryGetValue(deviceName, out SharedRecording recording) || recording == null || recording.Clip == null)
        {
            return -1;
        }

        return recording.Clip.GetInstanceID();
    }

    private int GetSharedUseCount()
    {
        if (string.IsNullOrEmpty(deviceName))
        {
            return -1;
        }

        if (!SharedRecordings.TryGetValue(deviceName, out SharedRecording recording) || recording == null)
        {
            return -1;
        }

        return recording.UseCount;
    }
}
