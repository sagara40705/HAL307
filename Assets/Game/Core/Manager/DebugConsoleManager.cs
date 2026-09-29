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

    protected override IEnumerator OnInitialize()
    {
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

        GUILayout.BeginArea(new Rect(10, 10, 360, 120), GUI.skin.box);

        GUILayout.Label($"[DebugConsole] FPS: {_fps:F1}");
        GUILayout.Label($"Scene: {sceneName}");

        GUILayout.Label("F1: Toggle Overlay");
        GUILayout.EndArea();
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
