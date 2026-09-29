using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MicrophoneInput : MonoBehaviour
{
    [SerializeField] private int sampleRate = 16000;
    [SerializeField] private int recordLengthSec = 1;

    private string deviceName;
    private int sampleCount;
    private float[] sampleBuffer;
    private SharedRecording sharedRecording;
    private Coroutine recordingCoroutine;
    private bool isUsingSharedRecording;
    private int observedGeneration = -1;
    private int observedClipId = -1;
    private float nextSampleStatsLogTime;
    private float nextMicCompareLogTime;

    private sealed class SharedRecording
    {
        public string DeviceName;
        public AudioClip CurrentClip;
        public int Generation;
        public int UseCount;
    }

    private static readonly Dictionary<string, SharedRecording> SharedRecordings = new();

    public int CurrentGeneration => sharedRecording != null ? sharedRecording.Generation : -1;

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
                deviceName = SelectMicrophoneDevice();
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

        bool created = false;

        if (!SharedRecordings.TryGetValue(deviceName, out sharedRecording) || sharedRecording == null)
        {
            Debug.Log("使用するマイク: " + deviceName);

            sharedRecording = new SharedRecording
            {
                DeviceName = deviceName,
                CurrentClip = null,
                Generation = 0,
                UseCount = 0
            };

            SharedRecordings[deviceName] = sharedRecording;
            created = true;
            Debug.Log($"[MicDebug] Created shared recording. object={gameObject.name}, deviceName={deviceName}");
            LogSharedMicTrace("AcquireSharedRecording created shared recording");
        }
        else
        {
            sharedRecording.DeviceName = deviceName;
            Debug.Log($"[MicDebug] Reusing shared recording. object={gameObject.name}, deviceName={deviceName}");
            LogSharedMicTrace("AcquireSharedRecording reusing shared recording");
        }

        if (sharedRecording.CurrentClip == null || !Microphone.IsRecording(deviceName))
        {
            if (!TryStartSharedRecording(sharedRecording, isRestart: !created))
            {
                if (created && sharedRecording.UseCount == 0)
                {
                    SharedRecordings.Remove(deviceName);
                }

                sharedRecording = null;
                return;
            }
        }

        sharedRecording.UseCount++;
        isUsingSharedRecording = true;
        observedGeneration = sharedRecording.Generation;
        observedClipId = GetClipId(sharedRecording.CurrentClip);
        Debug.Log($"[MicDebug] Joined shared recording. object={gameObject.name}, deviceName={deviceName}, useCount={sharedRecording.UseCount}");
        LogSharedMicSession("Acquire", sharedRecording);
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
            deviceName = SelectMicrophoneDevice();
            LogSharedMicTrace("RestartRecordingIfNeeded assigned deviceName");
        }

        if (!isUsingSharedRecording || sharedRecording == null)
        {
            AcquireSharedRecording();
            return;
        }

        if (!Microphone.IsRecording(deviceName) || sharedRecording.CurrentClip == null)
        {
            Debug.LogWarning("マイクが停止していたので再開します。");
            TryStartSharedRecording(sharedRecording, isRestart: true);
            LogSharedMicTrace("RestartRecordingIfNeeded after recreate");
        }
    }

    public float[] GetLatestSamples()
    {
        Debug.Log($"[MicDebug] GetLatestSamples called. object={gameObject.name}, deviceName={deviceName}, isUsingShared={isUsingSharedRecording}");
        LogSharedMicTrace("GetLatestSamples enter");

        RestartRecordingIfNeeded();
        LogSharedMicTrace("GetLatestSamples after RestartRecordingIfNeeded");

        SharedRecording currentRecording = sharedRecording;
        AudioClip currentClip = currentRecording?.CurrentClip;

        if (currentClip == null || sampleBuffer == null)
        {
            Debug.Log($"[MicDebug] GetLatestSamples return null. object={gameObject.name}, clipNull={currentClip == null}, bufferNull={sampleBuffer == null}");
            LogSharedMicTrace("GetLatestSamples return null clip or buffer");
            return null;
        }

        LogReadWhenSessionChanges(currentRecording);

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
            startPosition += currentClip.samples;
        }

        currentClip.GetData(sampleBuffer, startPosition);

        LogMicCompareIfNeeded(sampleBuffer, micPosition, startPosition, currentClip, currentRecording.Generation);
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

    private void LogMicCompareIfNeeded(float[] samples, int currentPosition, int startPosition, AudioClip currentClip, int generation)
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
        int localClipId = GetClipId(currentClip);
        int sharedClipId = GetSharedClipId();
        int clipSamples = currentClip != null ? currentClip.samples : -1;
        int frequency = currentClip != null ? currentClip.frequency : -1;
        int channels = currentClip != null ? currentClip.channels : -1;

        Debug.Log(
            $"[MicCompare] " +
            $"object={gameObject.name} " +
            $"scene={gameObject.scene.name} " +
            $"device={deviceName} " +
            $"localClipId={localClipId} " +
            $"sharedClipId={sharedClipId} " +
            $"sameClip={localClipId != -1 && localClipId == sharedClipId} " +
            $"generation={generation} " +
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
            sharedRecording = null;
            isUsingSharedRecording = false;
            observedGeneration = -1;
            observedClipId = -1;
            LogSharedMicTrace("ReleaseSharedRecording skip not using shared");
            return;
        }

        SharedRecording releasingRecording = sharedRecording;
        int beforeUseCount = releasingRecording.UseCount;
        releasingRecording.UseCount = Mathf.Max(0, releasingRecording.UseCount - 1);
        Debug.LogWarning($"[SharedMicTrace] ReleaseSharedRecording decremented. object={gameObject.name}, scene={gameObject.scene.name}, device={deviceName}, beforeUseCount={beforeUseCount}, afterUseCount={releasingRecording.UseCount}, sharedClipId={GetClipId(releasingRecording.CurrentClip)}, generation={releasingRecording.Generation}");
        LogSharedMicSession("Release", releasingRecording);

        if (releasingRecording.UseCount == 0)
        {
            if (Microphone.IsRecording(deviceName))
            {
                Microphone.End(deviceName);
                Debug.LogWarning("マイク録音終了");
                Debug.LogWarning($"[SharedMicTrace] ReleaseSharedRecording Microphone.End called. object={gameObject.name}, scene={gameObject.scene.name}, device={deviceName}");
            }

            if (SharedRecordings.TryGetValue(deviceName, out SharedRecording currentRecording) &&
                ReferenceEquals(currentRecording, releasingRecording))
            {
                SharedRecordings.Remove(deviceName);
            }

            releasingRecording.CurrentClip = null;
            Debug.LogWarning($"[SharedMicTrace] ReleaseSharedRecording removed shared recording. object={gameObject.name}, scene={gameObject.scene.name}, device={deviceName}");
        }

        sharedRecording = null;
        isUsingSharedRecording = false;
        observedGeneration = -1;
        observedClipId = -1;
        LogSharedMicTrace("ReleaseSharedRecording exit");
    }

    private bool TryStartSharedRecording(SharedRecording recording, bool isRestart)
    {
        if (recording == null || string.IsNullOrEmpty(recording.DeviceName))
        {
            return false;
        }

        string recordingDeviceName = recording.DeviceName;
        int previousClipId = GetClipId(recording.CurrentClip);
        int previousGeneration = recording.Generation;

        // A running device with no current clip is an invalid shared session.
        // End it before starting again so Microphone.Start is never duplicated.
        if (Microphone.IsRecording(recordingDeviceName))
        {
            if (recording.CurrentClip != null)
            {
                return true;
            }

            Microphone.End(recordingDeviceName);
        }

        recording.CurrentClip = null;
        AudioClip newClip = Microphone.Start(recordingDeviceName, true, recordLengthSec, sampleRate);

        if (newClip == null)
        {
            Debug.LogError($"マイク録音開始に失敗しました。device={recordingDeviceName}");
            return false;
        }

        recording.CurrentClip = newClip;
        recording.Generation++;
        Debug.Log("マイク録音開始");

        if (isRestart)
        {
            Debug.LogWarning(
                $"[SharedMicSession] object={gameObject.name} " +
                $"device={recordingDeviceName} " +
                $"generation={recording.Generation} " +
                $"clipId={GetClipId(recording.CurrentClip)} " +
                $"useCount={recording.UseCount} " +
                $"action=Restart " +
                $"previousGeneration={previousGeneration} " +
                $"previousClipId={previousClipId}"
            );
        }

        return true;
    }

    private void LogReadWhenSessionChanges(SharedRecording recording)
    {
        if (recording == null)
        {
            return;
        }

        int currentClipId = GetClipId(recording.CurrentClip);
        if (observedGeneration == recording.Generation && observedClipId == currentClipId)
        {
            return;
        }

        observedGeneration = recording.Generation;
        observedClipId = currentClipId;
        LogSharedMicSession("Read", recording);
    }

    private void LogSharedMicSession(string action, SharedRecording recording)
    {
        Debug.Log(
            $"[SharedMicSession] object={gameObject.name} " +
            $"device={(recording != null ? recording.DeviceName : deviceName)} " +
            $"generation={(recording != null ? recording.Generation : -1)} " +
            $"clipId={GetClipId(recording?.CurrentClip)} " +
            $"useCount={(recording != null ? recording.UseCount : -1)} " +
            $"action={action}"
        );
    }

    private static int GetClipId(AudioClip clip)
    {
        return clip != null ? clip.GetInstanceID() : -1;
    }

    private void LogSharedMicTrace(string point)
    {
        bool hasDevice = !string.IsNullOrEmpty(deviceName);
        bool isRecording = hasDevice && Microphone.IsRecording(deviceName);
        int position = hasDevice ? Microphone.GetPosition(deviceName) : -1;
        AudioClip currentClip = sharedRecording?.CurrentClip;
        int localClipId = GetClipId(currentClip);
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
            $"localClipNull={currentClip == null} " +
            $"localClipId={localClipId} " +
            $"sharedClipId={sharedClipId} " +
            $"sameClip={sameClip} " +
            $"generation={(sharedRecording != null ? sharedRecording.Generation : -1)} " +
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

        if (!SharedRecordings.TryGetValue(deviceName, out SharedRecording recording) || recording == null || recording.CurrentClip == null)
        {
            return -1;
        }

        return recording.CurrentClip.GetInstanceID();
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

    private string SelectMicrophoneDevice()
    {
        string[] devices = Microphone.devices;

        if (devices == null || devices.Length == 0)
        {
            return string.Empty;
        }

        string selectedDevice = null;
        string reason = string.Empty;

        for (int i = 0; i < devices.Length; i++)
        {
            string device = devices[i];

            if (ContainsIgnoreCase(device, "HyperX") || ContainsIgnoreCase(device, "SoloCast"))
            {
                selectedDevice = device;
                reason = "preferred external microphone";
                break;
            }
        }

        if (string.IsNullOrEmpty(selectedDevice))
        {
            for (int i = 0; i < devices.Length; i++)
            {
                string device = devices[i];

                if (LooksLikeExternalMicrophone(device) && !LooksLikeLowPriorityMicrophone(device))
                {
                    selectedDevice = device;
                    reason = "external microphone candidate";
                    break;
                }
            }
        }

        if (string.IsNullOrEmpty(selectedDevice))
        {
            selectedDevice = devices[0];
            reason = "fallback first available device";
        }

        LogMicrophoneDeviceSelection(devices, selectedDevice, reason);
        return selectedDevice;
    }

    private bool LooksLikeExternalMicrophone(string device)
    {
        return ContainsIgnoreCase(device, "USB")
            || ContainsIgnoreCase(device, "External")
            || ContainsIgnoreCase(device, "Headset")
            || ContainsIgnoreCase(device, "Mic")
            || ContainsIgnoreCase(device, "マイク");
    }

    private bool LooksLikeLowPriorityMicrophone(string device)
    {
        return ContainsIgnoreCase(device, "Realtek");
    }

    private bool ContainsIgnoreCase(string text, string value)
    {
        return !string.IsNullOrEmpty(text)
            && !string.IsNullOrEmpty(value)
            && text.IndexOf(value, System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void LogMicrophoneDeviceSelection(string[] devices, string selectedDevice, string reason)
    {
        string message = "[MicDeviceSelect]\ndevices:";

        for (int i = 0; i < devices.Length; i++)
        {
            message += $"\n{i}: {devices[i]}";
        }

        message += $"\nselected={selectedDevice}\nreason={reason}";
        Debug.Log(message);
    }
}
