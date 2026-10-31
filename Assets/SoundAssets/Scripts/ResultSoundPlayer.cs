using UnityEngine;
using static Sound.Manager.SoundManager;

public class ResultSoundPlayer : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Sound.Manager.SoundManager.Play((int)SceneSoundTag.ResultSound, (int)ResultSoundTag.ResultSE);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
