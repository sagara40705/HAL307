using UnityEngine;
using UnityEngine.InputSystem;

public sealed class SoundTest : MonoBehaviour
{
    private void Start()
    {
        SoundManager.Instance.PlayBgm(BgmId.Title);
    }


    private void Update()
    {
        if (DebugShortcuts.Combo(Key.S, Key.F3))
        {
            SoundManager.Instance.PlaySe(SeId.Sampling_1);
            Debug.Log("Play SE: Sampling_1");
            return;
        }

        if (DebugShortcuts.Combo(Key.S, Key.F2))
        {
            SoundManager.Instance.PlaySe(SeId.Sampling_2);
            Debug.Log("Play SE: Sampling_2");
            return;
        }

        if (DebugShortcuts.Combo(Key.B, Key.F2))
        {
            SoundManager.Instance.PlayBgm(BgmId.InGame, 0.5f);
            Debug.Log("Play BGM: InGame");
            return;
        }

        if (DebugShortcuts.Combo(Key.B, Key.S))
        {
            SoundManager.Instance.StopBgm(0.5f);
            Debug.Log("StopBgm");
            return;
        }

        if (DebugShortcuts.Combo(Key.B, Key.V))
        {
            SoundManager.Instance.SetBgmVolume(0.3f);
            Debug.Log("Set BGM Volume: 0.3");
            return;
        }

        if (DebugShortcuts.Combo(Key.B, Key.R))
        {
            SoundManager.Instance.SetBgmVolume(1.0f);
            Debug.Log("Set BGM Volume: 1.0");
            return;
        }
    }
}
