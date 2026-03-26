using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public sealed class DebugConsoleManager : AppManagerBase<DebugConsoleManager>
{
    [Header("Overlay")]
    [SerializeField] private bool showOverlay = true;

    private const float UpdateInterval = 0.5f;
    private static readonly Key ToggleKey = Key.F1;

    private float _fps;
    private float _timer;
    private int _frames;
    private bool _isLogHooked;

    protected override IEnumerator OnInitialize()
    {
        // 初期化が複数回呼ばれても安全にする
        if (!_isLogHooked)
        {
            Application.logMessageReceived += OnLogMessageReceived;
            _isLogHooked = true;
        }

        ResetFpsCounter();

        Debug.Log("[DebugConsoleManager] Initialize completed.");
        yield break;
    }

    private void Update()
    {
        // 未初期化中は動かさない
        if (!IsInitialized)
            return;

        if (Keyboard.current != null && Keyboard.current[ToggleKey].wasPressedThisFrame)
        {
            showOverlay = !showOverlay;
        }

        if (!showOverlay)
            return;

        _frames++;
        _timer += Time.unscaledDeltaTime;

        if (_timer >= UpdateInterval)
        {
            _fps = _frames / _timer;
            ResetFpsCounter(keepFps: true);
        }
    }

    private void OnGUI()
    {
        if (!IsInitialized || !showOverlay)
            return;

        string sceneName = SceneManager.GetActiveScene().name;

        // SceneLoader も新基底に合わせた前提
        //bool hasLoader = SceneLoader.TryGetInstance(out var loader);

        GUILayout.BeginArea(new Rect(10, 10, 360, 120), GUI.skin.box);

        GUILayout.Label($"[DebugConsole] FPS: {_fps:F1}");
        GUILayout.Label($"Scene: {sceneName}");

        //if (hasLoader && loader != null)
        //{
        //    GUILayout.Label($"SceneLoader.IsLoading: {loader.IsLoading}");
        //    GUILayout.Label($"SceneLoader.Progress:  {loader.Progress:P0} ({loader.Progress:F3})");
        //    GUILayout.Label($"SceneLoader.Target:    {loader.TargetSceneName}");
        //}
        //else
        //{
        //    GUILayout.Label("SceneLoader: (not created)");
        //}

        GUILayout.Label("F1: Toggle Overlay");
        GUILayout.EndArea();
    }

    private void OnLogMessageReceived(string condition, string stackTrace, LogType type)
    {
        // 将来ログ履歴を保持するならここで処理
    }

    protected override void OnDestroy()
    {
        if (_isLogHooked)
        {
            Application.logMessageReceived -= OnLogMessageReceived;
            _isLogHooked = false;
        }

        base.OnDestroy();
    }

    private void ResetFpsCounter(bool keepFps = false)
    {
        _frames = 0;
        _timer = 0f;

        if (!keepFps)
        {
            _fps = 0f;
        }
    }
}
