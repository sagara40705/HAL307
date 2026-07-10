using UnityEngine;

public class MicCheck : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("=== MicCheck Start ===");
        Debug.Log("マイク数: " + Microphone.devices.Length);

        foreach (string device in Microphone.devices)
        {
            Debug.Log("マイク名: " + device);
        }
    }
}
