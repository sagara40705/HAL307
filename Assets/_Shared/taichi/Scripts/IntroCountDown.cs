using System;
using System.Collections;
using Sound.Manager;
using UnityEngine;
using UnityEngine.UI;
using static Sound.Manager.SoundManager;

public class IntroCoutDown : MonoBehaviour
{
    [SerializeField] private Image[] _imageCountdown;  // カウントダウンの画像
    private int pCountDownNumber = 3;       // カウント数
    private float CountDownTimer = 1.0f;    // カウントダウン更新時間
    private float timer = 0.0f;
    private bool CountDownStart = false;    // カウントダウンスタート
    private bool isFinished = false;        // カウントダウン終了処理
    private Sound.Manager.SoundManager _soundManager;

    public void StartCountDown()
    {
        CountDownStart = true;
        _imageCountdown[pCountDownNumber].gameObject.SetActive(true);
        Sound.Manager.SoundManager.Play((int)SceneSoundTag.IntroSound, (int)IntroSoundTag.CountDown);
    }

    private void Update()
    {
        if (isFinished)
        {
            return;
        }

        if (!CountDownStart)
        {
            return;
        }

        timer += Time.deltaTime;

        if (timer >= CountDownTimer)
        {
            if (pCountDownNumber >= 0)
            {
                _imageCountdown[pCountDownNumber].gameObject.SetActive(false);
                pCountDownNumber--;
                timer = 0f;
                if (pCountDownNumber >= 0)
                {
                    _imageCountdown[pCountDownNumber].gameObject.SetActive(true);
                }
            }
            else
            {
                isFinished = true;
                _imageCountdown[0].gameObject.SetActive(false);
            }
        }
    }
}
