using System.Collections;
using UnityEngine;

public class MicrophoneInput : MonoBehaviour
{
    [SerializeField] private int sampleRate = 16000;
    [SerializeField] private int recordLengthSec = 1;

    private AudioClip microphoneClip;
    private string deviceName;
    private int sampleCount;

    private void Start()
    {
        sampleCount = sampleRate * recordLengthSec;
        StartCoroutine(StartRecordingWithRetry());
    }

    private IEnumerator StartRecordingWithRetry()
    {
        yield return new WaitForSeconds(1.0f);

        for (int i = 0; i < 10; i++)
        {
            Debug.Log($"マイク確認 {i + 1}回目 / マイク数: {Microphone.devices.Length}");

            if (Microphone.devices.Length > 0)
            {
                deviceName = Microphone.devices[0];
                StartRecording();
                yield break;
            }

            yield return new WaitForSeconds(0.5f);
        }

        Debug.LogError("マイクが見つかりません。");
    }

    private void StartRecording()
    {
        Debug.Log("使用するマイク: " + deviceName);

        microphoneClip = Microphone.Start(
            deviceName,
            true,
            recordLengthSec,
            sampleRate
        );

        Debug.Log("マイク録音開始");
    }

    public float[] GetLatestSamples()
    {
        if (microphoneClip == null)
        {
            return null;
        }

        int micPosition = Microphone.GetPosition(deviceName);

        if (micPosition <= 0)
        {
            return null;
        }

        float[] samples = new float[sampleCount];

        int startPosition = micPosition - sampleCount;
        if (startPosition < 0)
        {
            startPosition += microphoneClip.samples;
        }

        microphoneClip.GetData(samples, startPosition);

        return samples;
    }

    private void OnDisable()
    {
        if (!string.IsNullOrEmpty(deviceName) && Microphone.IsRecording(deviceName))
        {
            Microphone.End(deviceName);
        }
    }
}
