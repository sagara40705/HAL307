using System.Collections;
using UnityEngine;

public class MicrophoneInput : MonoBehaviour
{
    private static AudioClip sharedMicrophoneClip;
    private static string sharedDeviceName;
    private static int activeClientCount;

    [SerializeField] private int sampleRate = 16000;
    [SerializeField] private int recordLengthSec = 1;

    private AudioClip microphoneClip;
    private string deviceName;
    private int sampleCount;
    private float[] sampleBuffer;
    private bool isRegisteredClient;

    private void OnEnable()
    {
        if (isRegisteredClient)
        {
            return;
        }

        activeClientCount++;
        isRegisteredClient = true;
    }

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
        if (TryAttachToSharedRecording())
        {
            return;
        }

        if (!string.IsNullOrEmpty(deviceName) && Microphone.IsRecording(deviceName))
        {
            Debug.LogWarning("[MicrophoneInput] Existing recording has no shared AudioClip. Restarting the device.");
            Microphone.End(deviceName);
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

        sharedDeviceName = deviceName;
        sharedMicrophoneClip = microphoneClip;

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
        if (TryAttachToSharedRecording())
        {
            return;
        }

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

    private bool TryAttachToSharedRecording()
    {
        if (sharedMicrophoneClip == null || string.IsNullOrEmpty(sharedDeviceName))
        {
            return false;
        }

        if (!Microphone.IsRecording(sharedDeviceName))
        {
            sharedMicrophoneClip = null;
            sharedDeviceName = null;
            return false;
        }

        deviceName = sharedDeviceName;
        microphoneClip = sharedMicrophoneClip;
        return true;
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

        if (isRegisteredClient)
        {
            activeClientCount = Mathf.Max(0, activeClientCount - 1);
            isRegisteredClient = false;
        }

        microphoneClip = null;

        if (activeClientCount > 0)
        {
            return;
        }

        if (!string.IsNullOrEmpty(sharedDeviceName) && Microphone.IsRecording(sharedDeviceName))
        {
            Microphone.End(sharedDeviceName);
            Debug.LogWarning("マイク録音終了");
        }

        sharedMicrophoneClip = null;
        sharedDeviceName = null;
    }
}
