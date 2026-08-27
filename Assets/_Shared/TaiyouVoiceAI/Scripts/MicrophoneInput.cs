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
        if (isUsingSharedRecording)
        {
            return;
        }

        if (string.IsNullOrEmpty(deviceName))
        {
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
        }
        else
        {
            Debug.Log($"[MicDebug] Reusing shared recording. object={gameObject.name}, deviceName={deviceName}");
        }

        sharedRecording.UseCount++;
        microphoneClip = sharedRecording.Clip;
        isUsingSharedRecording = true;
        Debug.Log($"[MicDebug] Joined shared recording. object={gameObject.name}, deviceName={deviceName}, useCount={sharedRecording.UseCount}");
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
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("マイクが見つかりません。");
            return;
        }

        if (string.IsNullOrEmpty(deviceName))
        {
            deviceName = Microphone.devices[0];
        }

        if (!Microphone.IsRecording(deviceName))
        {
            Debug.LogWarning("マイクが停止していたので再開します。");

            if (isUsingSharedRecording && sharedRecording != null)
            {
                sharedRecording.UseCount = Mathf.Max(0, sharedRecording.UseCount - 1);
                isUsingSharedRecording = false;
            }

            microphoneClip = null;
            sharedRecording = null;
            SharedRecordings.Remove(deviceName);
            AcquireSharedRecording();
        }
    }

    public float[] GetLatestSamples()
    {
        Debug.Log($"[MicDebug] GetLatestSamples called. object={gameObject.name}, deviceName={deviceName}, isUsingShared={isUsingSharedRecording}");

        RestartRecordingIfNeeded();

        if (microphoneClip == null || sampleBuffer == null)
        {
            Debug.Log($"[MicDebug] GetLatestSamples return null. object={gameObject.name}, clipNull={microphoneClip == null}, bufferNull={sampleBuffer == null}");
            return null;
        }

        int micPosition = Microphone.GetPosition(deviceName);
        Debug.Log($"[MicDebug] Microphone.GetPosition. object={gameObject.name}, deviceName={deviceName}, position={micPosition}");

        if (micPosition <= 0)
        {
            Debug.Log($"[MicDebug] GetLatestSamples return null. object={gameObject.name}, reason=micPosition<=0");
            return null;
        }

        int startPosition = micPosition - sampleCount;

        if (startPosition < 0)
        {
            startPosition += microphoneClip.samples;
        }

        microphoneClip.GetData(sampleBuffer, startPosition);

        Debug.Log($"[MicDebug] GetLatestSamples returned. object={gameObject.name}, samples={sampleBuffer.Length}, startPosition={startPosition}");
        return sampleBuffer;
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

        if (recordingCoroutine != null)
        {
            StopCoroutine(recordingCoroutine);
            recordingCoroutine = null;
        }

        ReleaseSharedRecording();
        Debug.LogWarning($"MicrophoneInput disabled. object={gameObject.name}, scene={gameObject.scene.name}, activeSelf={gameObject.activeSelf}, activeInHierarchy={gameObject.activeInHierarchy}");
    }

    private void ReleaseSharedRecording()
    {
        if (!isUsingSharedRecording || string.IsNullOrEmpty(deviceName) || sharedRecording == null)
        {
            microphoneClip = null;
            sharedRecording = null;
            isUsingSharedRecording = false;
            return;
        }

        sharedRecording.UseCount = Mathf.Max(0, sharedRecording.UseCount - 1);

        if (sharedRecording.UseCount == 0)
        {
            if (Microphone.IsRecording(deviceName))
            {
                Microphone.End(deviceName);
                Debug.LogWarning("マイク録音終了");
            }

            SharedRecordings.Remove(deviceName);
        }

        microphoneClip = null;
        sharedRecording = null;
        isUsingSharedRecording = false;
    }
}
