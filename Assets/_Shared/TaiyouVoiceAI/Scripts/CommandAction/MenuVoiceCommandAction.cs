using UnityEngine;

public class MenuVoiceCommandAction : MonoBehaviour
{
    private const string DebugWindowName = "Voice Command";
    private const string CommandTime = "time";
    private const string CommandUp = "ue";
    private const string CommandDown = "sita";
    private const string CommandDecision = "kettei";
    private const string CommandNone = "none";

    [Header("Pause Menu / Menu Controller")]
    [Tooltip("Assign a component that has OnVoiceCommand(string command).")]
    [SerializeField] private MonoBehaviour menuController;

    [Header("Stage Select")]
    [Tooltip("Legacy fallback target that has OnVoiceCommand(string command).")]
    [SerializeField] private MonoBehaviour stageSelectController;

    [Header("Time Command")]
    [SerializeField] private bool requireTimeBeforeMenuControl = true;
    [SerializeField] private float menuControlEnableTime = 5.0f;

    private bool menuControlEnabled;
    private float menuControlTimer;

    private void Start()
    {
        if (menuController == null)
        {
            menuController = GetComponent<VoiceMainMenuController>();
        }

        CreateDebugMenu();
    }

    private void OnDestroy()
    {
        RuntimeDebug.DebugMenu.DestroyWindow(DebugWindowName);
    }

    private void Update()
    {
        if (!menuControlEnabled)
        {
            return;
        }

        if (menuControlEnableTime <= 0f)
        {
            return;
        }

        menuControlTimer -= Time.unscaledDeltaTime;
        if (menuControlTimer <= 0f)
        {
            menuControlEnabled = false;
            Debug.Log("[Menu Voice] Time command window expired.");
        }
    }

    public void ExecuteCommand(string command, float volume)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return;
        }

        command = command.Trim().ToLowerInvariant();

        if (command == CommandNone)
        {
            return;
        }

        if (command == CommandTime)
        {
            EnableMenuControl();
            return;
        }

        if (requireTimeBeforeMenuControl && !menuControlEnabled)
        {
            Debug.Log($"[Menu Voice] Command skipped. Reason=Time command is required first. Command={command}");
            return;
        }

        ExecuteCommandByPhase(command);
    }

    public void DebugExecuteTimeCommand()
    {
        ExecuteCommand(CommandTime, 0f);
    }

    public void DebugExecuteUpCommand()
    {
        ExecuteCommand(CommandUp, 0f);
    }

    public void DebugExecuteDownCommand()
    {
        ExecuteCommand(CommandDown, 0f);
    }

    public void DebugExecuteDecisionCommand()
    {
        ExecuteCommand(CommandDecision, 0f);
    }

    private void EnableMenuControl()
    {
        menuControlEnabled = true;
        menuControlTimer = menuControlEnableTime;

        GamePhase currentPhase = GetCurrentPhase();
        Debug.Log($"[Menu Voice] Time command accepted. Phase={currentPhase}");

        if (currentPhase == GamePhase.InGame)
        {
            TryChangePhase(GamePhase.Pause);
            return;
        }

        if (currentPhase == GamePhase.Pause)
        {
            return;
        }

        if (currentPhase == GamePhase.None)
        {
            OpenPauseFallback();
        }
    }

    private void ExecuteCommandByPhase(string command)
    {
        GamePhase currentPhase = GetCurrentPhase();

        switch (currentPhase)
        {
            case GamePhase.Title:
                ExecuteTitleCommand(command);
                break;

            case GamePhase.Pause:
                ExecutePauseCommand(command);
                break;

            case GamePhase.InGame:
                Debug.Log($"[Menu Voice] Command skipped. Reason=InGame phase does not use menu commands. Command={command}");
                break;

            case GamePhase.Result:
                Debug.Log($"[Menu Voice] Command skipped. Reason=Result phase does not use menu commands. Command={command}");
                break;

            default:
                ExecuteFallbackCommand(command);
                break;
        }
    }

    private void ExecuteTitleCommand(string command)
    {
        if (command == CommandDecision)
        {
            TryChangePhase(GamePhase.InGame);
            return;
        }

        Debug.Log($"[Menu Voice] Command skipped. Reason=Title phase only supports decision. Command={command}");
    }

    private void ExecutePauseCommand(string command)
    {
        switch (command)
        {
            case CommandUp:
            case CommandDown:
            case CommandDecision:
                SendCommandTo(menuController, command);
                break;

            default:
                Debug.Log($"[Menu Voice] Command skipped. Reason=Pause phase unsupported command. Command={command}");
                break;
        }
    }

    private void ExecuteFallbackCommand(string command)
    {
        switch (command)
        {
            case CommandUp:
            case CommandDown:
            case CommandDecision:
                SendCommandTo(menuController, command);
                SendCommandTo(stageSelectController, command);
                break;

            default:
                Debug.Log($"[Menu Voice] Unsupported command. Command={command}");
                break;
        }
    }

    private bool TryChangePhase(GamePhase nextPhase)
    {
        GamePhaseManager gamePhaseManager = GamePhaseManager.Instance;
        if (gamePhaseManager == null || !gamePhaseManager.IsInitialized)
        {
            Debug.LogWarning($"[Menu Voice] Phase change skipped. Reason=GamePhaseManager is not ready. NextPhase={nextPhase}");
            return false;
        }

        return gamePhaseManager.ChangePhase(nextPhase);
    }

    private GamePhase GetCurrentPhase()
    {
        GamePhaseManager gamePhaseManager = GamePhaseManager.Instance;
        if (gamePhaseManager != null && gamePhaseManager.IsInitialized)
        {
            return gamePhaseManager.CurrentPhase;
        }

        PauseManager pauseManager = PauseManager.Instance;
        if (pauseManager != null && pauseManager.IsPaused)
        {
            return GamePhase.Pause;
        }

        return GamePhase.None;
    }

    private void OpenPauseFallback()
    {
        PauseManager pauseManager = PauseManager.Instance;
        if (pauseManager != null && pauseManager.IsInitialized)
        {
            pauseManager.OpenPause();
            return;
        }

        SendCommandTo(menuController, CommandTime);
    }

    private void SendCommandTo(MonoBehaviour target, string command)
    {
        if (target == null)
        {
            Debug.Log($"[Menu Voice] Command target is empty. Command={command}");
            return;
        }

        target.SendMessage("OnVoiceCommand", command, SendMessageOptions.DontRequireReceiver);
    }

    private void CreateDebugMenu()
    {
        RuntimeDebug.DebugWindow window = RuntimeDebug.DebugMenu.CreateWindow(DebugWindowName);
        window.CreateButton("\u30bf\u30a4\u30e0", DebugExecuteTimeCommand);
        window.CreateButton("\u4e0a", DebugExecuteUpCommand);
        window.CreateButton("\u4e0b", DebugExecuteDownCommand);
        window.CreateButton("\u6c7a\u5b9a", DebugExecuteDecisionCommand);
    }
}
