using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class IntroCoutDown : MonoBehaviour
{
    [SerializeField] private Image[] _imageCountdown;
    private int pCountDownNumber = 3;
    private float CountDownTimer = 1.0f;
    private bool finishCountDown = false;
    private float timer = 0.0f;
    private bool isFinished = false;

    public void StartCountDown()
    {
        finishCountDown = true;
        _imageCountdown[pCountDownNumber].gameObject.SetActive(true);
    }

    private void Update()
    {
        if (isFinished)
        {
            return;
        }

        if (!finishCountDown)
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
