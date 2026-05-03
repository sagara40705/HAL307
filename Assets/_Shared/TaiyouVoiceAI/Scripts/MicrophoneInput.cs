using UnityEngine;

public class MicrophoneInput : MonoBehaviour
{
    [SerializeField] private int sampleRate = 16000;
    [SerializeField] private int recordLengthSec = 1;

    private AudioClip micClip;
    private string micDevice;

    public int SampleRate => sampleRate;
    public int RecordLengthSec => recordLengthSec;

    private void Start()
    {
        StartRecording();
    }

    public void StartRecording()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("マイクが見つかりません");
            return;
        }

        micDevice = Microphone.devices[0];
        Debug.Log("使用マイク: " + micDevice);

        micClip = Microphone.Start(micDevice, true, recordLengthSec, sampleRate);
    }

    public float[] GetLatestSamples()
    {
        if (micClip == null)
        {
            Debug.LogWarning("マイク録音が開始されていません");
            return null;
        }

        int sampleCount = micClip.samples;
        float[] samples = new float[sampleCount];
        micClip.GetData(samples, 0);
        return samples;
    }

    public bool IsRecording()
    {
        return micClip != null && Microphone.IsRecording(micDevice);
    }

    private void OnDisable()
    {
        if (!string.IsNullOrEmpty(micDevice) && Microphone.IsRecording(micDevice))
        {
            Microphone.End(micDevice);
        }
    }
}