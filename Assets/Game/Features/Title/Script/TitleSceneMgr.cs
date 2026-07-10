using System;
using UnityEngine;

public class TitleSceneMgr : MonoBehaviour
{
    [SerializeField] private string _nextSceneName = "GameScene";
    [SerializeField] private UnityEngine.InputSystem.Key _key = UnityEngine.InputSystem.Key.None;

    [Header("音声認識設定")]
    [SerializeField, Range(0f, 1f)]
    private float _minimumConfidence = 0.7f;

    [SerializeField]
    private float _voiceCooldown = 1.0f;

    private float _lastVoiceTime = -999f;
    private bool _isStarting;

    [SerializeField] private MenuVoiceCommandAction voiceCommandAction;
    [SerializeField] private TitleSceneMgr titleSceneMgr;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // デバッグ用のスペースキーで次のシーンに遷移
        if (DebugShortcuts.Pressed(_key))
        {
            StartGame();
        }

        if (DebugShortcuts.Pressed(UnityEngine.InputSystem.Key.LeftCtrl))
        {
            PauseManager.Instance.ClosePause();
        }

    }

     void OnVoiceCommand(string command, float confidence = 1.0f)
    {
        if (string.IsNullOrWhiteSpace(command))
            return;

        if (confidence < _minimumConfidence)
            return;

        if (Time.unscaledTime - _lastVoiceTime < _voiceCooldown)
            return;

        command = command.Trim().ToLowerInvariant();

        if (command == "kettei")
        {
            _lastVoiceTime = Time.unscaledTime;
            StartGame();
        }
    }


    private void StartGame()
    {
        if (_isStarting)
            return;

        _isStarting = true;

        Debug.Log("タイトル画面：ゲーム開始");

        // 現在の開始処理
        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.OpenPause();
        }
        else
        {
            Debug.LogWarning("PauseManager.Instanceが見つかりません。");
            _isStarting = false;
        }

         SceneLoader.Load(_nextSceneName);
    }

    internal void OnVoiceCommand(string v)
    {
        throw new NotImplementedException();
    }
}


//  PauseManager.Instance.OpenPause();
//SceneLoader.Load(_nextSceneName);

//SceneLoader.Load(_nextSceneName);
