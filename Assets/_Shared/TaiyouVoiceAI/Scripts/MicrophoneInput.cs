using System.Collections;
using UnityEngine;

public class MicrophoneInput : MonoBehaviour
{
    [SerializeField] private int sampleRate = 16000;
    [SerializeField] private int recordLengthSec = 1;

    private AudioClip microphoneClip;
    private string deviceName;
    private int sampleCount;
    private float[] sampleBuffer;

    private void Start()
    {
        sampleCount = sampleRate * recordLengthSec;
        sampleBuffer = new float[sampleCount];

        StartCoroutine(StartRecordingWithRetry());
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
                StartRecording();
                yield break;
            }

            yield return new WaitForSecondsRealtime(0.5f);
        }

        Debug.LogError("マイクが見つかりません。");
    }

    private void StartRecording()
    {
        if (!string.IsNullOrEmpty(deviceName) && Microphone.IsRecording(deviceName))
        {
            return;
        }

        Debug.Log("使用するマイク: " + deviceName);

        microphoneClip = Microphone.Start(
            deviceName,
            true,
            recordLengthSec,
            sampleRate
        );

        if (microphoneClip == null)
        {
            Debug.LogError("Microphone.Start に失敗しました");
            return;
        }

        Debug.Log("マイク録音開始");
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
            StartRecording();
        }
    }

    public float[] GetLatestSamples()
    {
        RestartRecordingIfNeeded();

        if (microphoneClip == null || sampleBuffer == null)
        {
            return null;
        }

        int micPosition = Microphone.GetPosition(deviceName);

        if (micPosition <= 0)
        {
            return null;
        }

        int startPosition = micPosition - sampleCount;

        if (startPosition < 0)
        {
            startPosition += microphoneClip.samples;
        }

        microphoneClip.GetData(sampleBuffer, startPosition);

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

        if (!string.IsNullOrEmpty(deviceName) && Microphone.IsRecording(deviceName))
        {
            Microphone.End(deviceName);
            Debug.LogWarning("マイク録音終了");
        }
    }
}
