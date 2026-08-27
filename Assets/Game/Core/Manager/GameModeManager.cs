using System;
using System.Collections;
using UnityEngine;

public enum GameMode
{
    ThreePlayer = 3,
    FivePlayer = 5,
}

/// <summary>
/// アプリ全体のゲームモード選択と選択ロックを管理する。
/// </summary>
public sealed class GameModeManager : AppManagerBase<GameModeManager>
{
    private const string DebugWindowName = "Game Mode";

    [SerializeField] private GameMode defaultGameMode = GameMode.ThreePlayer;

    private GamePhaseManager _gamePhaseManager;
    private GameMode _currentGameMode;
    private bool _isSelectionLocked;

    public GameMode CurrentGameMode => _currentGameMode;
    public bool IsSelectionLocked => _isSelectionLocked;

    public event Action<GameMode, GameMode> GameModeChanged;

    protected override IEnumerator OnInitialize()
    {
        _currentGameMode = defaultGameMode;
        _gamePhaseManager = GamePhaseManager.Instance;

        if (_gamePhaseManager != null && _gamePhaseManager.IsInitialized)
        {
            _gamePhaseManager.PhaseChanged += OnPhaseChanged;
            GamePhase currentPhase = _gamePhaseManager.CurrentPhase;
            _isSelectionLocked = currentPhase != GamePhase.None && currentPhase != GamePhase.Title;
        }
        else
        {
            _isSelectionLocked = false;
            Debug.LogWarning("[GameModeManager] GamePhaseManager is not ready. StageModel will lock the selection when the mode is applied.");
        }

        CreateDebugMenu();
        Debug.Log($"[GameModeManager] Initialize completed. Mode={_currentGameMode}, Locked={_isSelectionLocked}");
        yield break;
    }

    protected override void OnDestroy()
    {
        if (_gamePhaseManager != null)
        {
            _gamePhaseManager.PhaseChanged -= OnPhaseChanged;
        }

        RuntimeDebug.DebugMenu.DestroyWindow(DebugWindowName);
        base.OnDestroy();
    }

    /// <summary>
    /// タイトル中にゲームモードを変更する。
    /// </summary>
    public bool TrySetGameMode(GameMode gameMode)
    {
        if (!IsInitialized)
        {
            Debug.LogWarning($"[GameModeManager] Game mode change skipped. Reason=Manager is not initialized. Mode={gameMode}");
            return false;
        }

        if (!Enum.IsDefined(typeof(GameMode), gameMode))
        {
            Debug.LogError($"[GameModeManager] Game mode change failed. Reason=Unknown mode. Mode={gameMode}");
            return false;
        }

        if (_currentGameMode == gameMode)
        {
            return true;
        }

        if (_isSelectionLocked)
        {
            Debug.LogWarning($"[GameModeManager] Game mode change skipped. Reason=Selection is locked. Current={_currentGameMode}, Requested={gameMode}");
            return false;
        }

        GameMode oldMode = _currentGameMode;
        _currentGameMode = gameMode;
        Debug.Log($"[GameModeManager] Game mode changed. From={oldMode}, To={_currentGameMode}");
        GameModeChanged?.Invoke(oldMode, _currentGameMode);
        return true;
    }

    // UnityEventから呼ぶ3P選択入口
    public void SelectThreePlayerMode()
    {
        TrySetGameMode(GameMode.ThreePlayer);
    }

    // UnityEventから呼ぶ5P選択入口
    public void SelectFivePlayerMode()
    {
        TrySetGameMode(GameMode.FivePlayer);
    }

    /// <summary>
    /// 現在の選択をタイトルへ戻るまで固定する。
    /// </summary>
    public void LockSelection()
    {
        if (_isSelectionLocked)
        {
            return;
        }

        _isSelectionLocked = true;
        Debug.Log($"[GameModeManager] Game mode selection locked. Mode={_currentGameMode}");
    }

    private void OnPhaseChanged(GamePhase oldPhase, GamePhase newPhase)
    {
        if (newPhase == GamePhase.Title)
        {
            UnlockSelection();
            return;
        }

        if (newPhase == GamePhase.InGame)
        {
            LockSelection();
        }
    }

    private void UnlockSelection()
    {
        if (!_isSelectionLocked)
        {
            return;
        }

        _isSelectionLocked = false;
        Debug.Log("[GameModeManager] Game mode selection unlocked.");
    }

    private void CreateDebugMenu()
    {
        RuntimeDebug.DebugWindow window = RuntimeDebug.DebugMenu.CreateWindow(DebugWindowName);
        window.CreateMessage("Current Mode", () => _currentGameMode.ToString());
        window.CreateMessage("Selection Locked", () => _isSelectionLocked.ToString());
        window.CreateButton("Select 3P", SelectThreePlayerMode);
        window.CreateButton("Select 5P", SelectFivePlayerMode);
    }
}
