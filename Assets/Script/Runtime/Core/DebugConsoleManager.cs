using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class DebugConsoleManager : AppSingletonMono<DebugConsoleManager>
{
    // オーバーレイ表示フラグ
    private bool showOverlay = true;

    private const float updateInterval = 0.5f;      // FPS更新間隔（秒）
    private static readonly Key toggleKey = Key.F1;   // オーバーレイ表示切替キー

    // FPS計測
    private float fps;
    private float timer;
    private int frames;

    protected override void Awake()
    {
        base.Awake();

        // ここに「起動時に一回だけやりたい初期化」を書く
        Application.logMessageReceived += OnLogMessageReceived;
    }
    private void Update()
    {
        // F1で表示切替（InputManager導入前でも動く）
        if (Keyboard.current != null && Keyboard.current[toggleKey].wasPressedThisFrame) showOverlay = !showOverlay;

        // オーバーレイ非表示なら処理しない
        if (!showOverlay) return;

        frames++;   // フレーム数カウント
        timer += Time.unscaledDeltaTime;    // 経過時間加算

        // 0.5秒ごとに更新
        if (timer >= updateInterval)
        {
            fps = frames / timer;
            frames = 0;
            timer = 0f;
        }
    }

    private void OnGUI()
    {
        if (!showOverlay) return;

        bool hasLoader = SceneLoader.TryGet(out var loader);
        string sceneName = SceneManager.GetActiveScene().name;

        // すごく簡易なオーバーレイ（後でUI(TextMeshPro)に変えてOK）
        GUILayout.BeginArea(new Rect(10, 10, 360, 120), GUI.skin.box);

        GUILayout.Label($"[DebugConsole] FPS: {fps:F1}");
        GUILayout.Label($"Scene: {sceneName}");

        if (hasLoader && loader != null)
        {
            // IsLoading: bool / Progress: 0〜1
            GUILayout.Label($"SceneLoader.IsLoading: {loader.IsLoading}");
            GUILayout.Label($"SceneLoader.Progress:  {loader.Progress:P0} ({loader.Progress:F3})");
            GUILayout.Label($"SceneLoader.Target:    {loader.TargetSceneName}");
        }
        else
        {
            GUILayout.Label("SceneLoader: (not created)");
        }

        GUILayout.Label("F1: Toggle Overlay");
        GUILayout.EndArea();
    }

    private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        // 今は何もしない（将来ログ履歴をためる）
    }

    private void OnDisable()
    {
        // 念のため解除（破壊禁止運用でも、ドメインリロード等に備える）
        Application.logMessageReceived -= OnLogMessageReceived;
    }
}