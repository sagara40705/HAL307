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
    public bool IsFinished => isFinished;
    public bool IsRunning => CountDownStart && !isFinished;

    public void StartCountDown()
    {
        if (IsRunning) { return; }
        if (_imageCountdown == null || _imageCountdown.Length < 4 ||
            Array.Exists(_imageCountdown, image => image == null))
        {
            throw new InvalidOperationException("[IntroCoutDown] Countdown images 0 through 3 are required.");
        }

        HideImages();
        pCountDownNumber = 3;
        timer = 0f;
        isFinished = false;
        CountDownStart = true;
        _imageCountdown[pCountDownNumber].gameObject.SetActive(true);
        Sound.Manager.SoundManager.Play((int)SceneSoundTag.IntroSound, (int)IntroSoundTag.CountDown);
    }

    public void CancelCountDown()
    {
        bool wasRunning = IsRunning;
        CountDownStart = false;
        isFinished = false;
        pCountDownNumber = 3;
        timer = 0f;
        HideImages();
        if (wasRunning)
        {
            Sound.Manager.SoundManager.Stop((int)SceneSoundTag.IntroSound, (int)IntroSoundTag.CountDown);
        }
    }

    private void HideImages()
    {
        if (_imageCountdown == null) { return; }
        foreach (Image image in _imageCountdown)
        {
            if (image != null) { image.gameObject.SetActive(false); }
        }
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
                CountDownStart = false;
                _imageCountdown[0].gameObject.SetActive(false);
                Sound.Manager.SoundManager.Stop((int)SceneSoundTag.IntroSound, (int)IntroSoundTag.Handclap);
                Sound.Manager.SoundManager.Play((int)SceneSoundTag.InGameSound, (int)InGameSoundTag.InGameBGM);
            }
        }
    }
}
