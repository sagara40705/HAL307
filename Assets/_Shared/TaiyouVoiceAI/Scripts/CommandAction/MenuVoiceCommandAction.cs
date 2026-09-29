using UnityEngine;

public class MenuVoiceCommandAction : MonoBehaviour
{
    private const string DebugWindowName = "Voice Command";
    private const string CommandTime = "time";
    private const string CommandUp = "ue";
    private const string CommandDown = "sita";
    private const string CommandLeft = "hidari";
    private const string CommandRight = "migi";
    private const string CommandDecision = "kettei";
    private const string CommandNone = "none";

    [Header("Pause Menu / Menu Controller")]
    [Tooltip("Assign a component that has OnVoiceCommand(string command).")]
    [SerializeField] private MonoBehaviour menuController;

    [Header("Pause Voice Routing")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject micSettingPanel;
    [SerializeField] private MonoBehaviour micSettingController;
    [SerializeField] private GameObject howToPanel;
    [SerializeField] private MonoBehaviour howToController;

    [Header("Stage Select")]
    [Tooltip("Legacy fallback target that has OnVoiceCommand(string command).")]
    [SerializeField] private MonoBehaviour stageSelectController;

    [Header("Time Command")]
    [SerializeField] private bool requireTimeBeforeMenuControl = true;
    [SerializeField] private float menuControlStartDelay = 1.0f;
    [SerializeField] private float menuControlEnableTime = 5.0f;

    private bool menuControlEnabled;
    private bool menuControlWaiting;
    private float menuControlStartDelayTimer;
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
        if (menuControlWaiting)
        {
            menuControlStartDelayTimer -= Time.unscaledDeltaTime;
            if (menuControlStartDelayTimer <= 0f)
            {
                menuControlWaiting = false;
                menuControlEnabled = true;
                menuControlTimer = menuControlEnableTime;
                Debug.Log("[Menu Voice] メニュー音声操作受付開始");
                Debug.Log($"[MenuVoiceTrace] Menu control started. duration={menuControlEnableTime:F3}, remaining={menuControlTimer:F3}");
            }

            return;
        }

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
            Debug.Log("[Menu Voice] メニュー音声操作受付終了");
            Debug.Log("[MenuVoiceTrace] Time command window expired.");
        }
    }

    public void ExecuteCommand(string command, float volume)
    {
        Debug.Log($"[MenuVoiceTrace] MenuVoiceCommandAction.ExecuteCommand entered. command={command}, volume={volume:F8}, menuControlEnabled={menuControlEnabled}, menuControlWaiting={menuControlWaiting}, startDelayRemaining={menuControlStartDelayTimer:F3}, menuControlTimer={menuControlTimer:F3}, requireTimeBeforeMenuControl={requireTimeBeforeMenuControl}, menuControllerNull={menuController == null}");

        if (string.IsNullOrWhiteSpace(command))
        {
            Debug.Log("[MenuVoiceTrace] MenuVoiceCommandAction return. reason=empty command");
            return;
        }

        command = command.Trim().ToLowerInvariant();
        Debug.Log($"[MenuVoiceTrace] MenuVoiceCommandAction normalized command={command}");

        if (command == CommandNone)
        {
            Debug.Log("[MenuVoiceTrace] MenuVoiceCommandAction return. reason=none");
            return;
        }

        if (command == CommandTime)
        {
            Debug.Log("[MenuVoiceTrace] MenuVoiceCommandAction time command received");
            EnableMenuControl();
            return;
        }

        if (requireTimeBeforeMenuControl && !menuControlEnabled)
        {
            if (menuControlWaiting)
            {
                Debug.Log($"[Menu Voice] Command skipped. Reason=Waiting after time command. Command={command}");
                Debug.Log($"[MenuVoiceTrace] MenuVoiceCommandAction return. reason=start delay active, command={command}, startDelayRemaining={menuControlStartDelayTimer:F3}");
            }
            else
            {
                Debug.Log($"[Menu Voice] Command skipped. Reason=Time command is required first. Command={command}");
                Debug.Log($"[MenuVoiceTrace] MenuVoiceCommandAction return. reason=time window disabled, command={command}, remaining={menuControlTimer:F3}");
            }

            return;
        }

        Debug.Log($"[MenuVoiceTrace] MenuVoiceCommandAction passing to phase handler. command={command}, menuControlEnabled={menuControlEnabled}, remaining={menuControlTimer:F3}");
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

    public void DebugExecuteLeftCommand()
    {
        ExecuteCommand(CommandLeft, 0f);
    }

    public void DebugExecuteRightCommand()
    {
        ExecuteCommand(CommandRight, 0f);
    }

    private void EnableMenuControl()
    {
        menuControlEnabled = false;
        menuControlWaiting = true;
        menuControlStartDelayTimer = Mathf.Max(0f, menuControlStartDelay);
        menuControlTimer = 0f;
        Debug.Log("[Menu Voice] 1秒待機開始");
        Debug.Log($"[MenuVoiceTrace] Time command accepted. startDelay={menuControlStartDelayTimer:F3}, controlDuration={menuControlEnableTime:F3}");

        GamePhase currentPhase = GetCurrentPhase();
        Debug.Log($"[Menu Voice] Time command accepted. Phase={currentPhase}");

        if (menuControlStartDelayTimer <= 0f)
        {
            menuControlWaiting = false;
            menuControlEnabled = true;
            menuControlTimer = menuControlEnableTime;
            Debug.Log("[Menu Voice] メニュー音声操作受付開始");
            Debug.Log($"[MenuVoiceTrace] Menu control started immediately. duration={menuControlEnableTime:F3}, remaining={menuControlTimer:F3}");
        }

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
        Debug.Log($"[MenuVoiceTrace] ExecuteCommandByPhase. command={command}, phase={currentPhase}, menuControlEnabled={menuControlEnabled}, remaining={menuControlTimer:F3}");

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
        Debug.Log($"[MenuVoiceTrace] ExecutePauseCommand entered. command={command}, menuControllerNull={menuController == null}");

        switch (command)
        {
            case CommandUp:
            case CommandDown:
            case CommandLeft:
            case CommandRight:
            case CommandDecision:
                RoutePauseCommand(command);
                break;

            default:
                Debug.Log($"[Menu Voice] Command skipped. Reason=Pause phase unsupported command. Command={command}");
                break;
        }
    }

    private void RoutePauseCommand(string command)
    {
        bool mainMenuActive = IsPanelActive(mainMenuPanel);
        bool micSettingActive = IsPanelActive(micSettingPanel);
        bool howToActive = IsPanelActive(howToPanel);
        int activeReceiverCount = (mainMenuActive ? 1 : 0) + (micSettingActive ? 1 : 0) + (howToActive ? 1 : 0);

        if (activeReceiverCount == 0)
        {
            Debug.Log($"[MenuVoiceBlocked] command={command} reason=NoActiveReceiver");
            return;
        }

        if (activeReceiverCount > 1)
        {
            Debug.Log($"[MenuVoiceBlocked] command={command} reason=MultipleActiveReceivers mainMenu={mainMenuActive} micSetting={micSettingActive} howTo={howToActive}");
            return;
        }

        if (micSettingActive)
        {
            RouteToActiveReceiver(command, micSettingController, "MicSetting");
            return;
        }

        if (howToActive)
        {
            RouteToActiveReceiver(command, howToController, "HowTo");
            return;
        }

        RouteToActiveReceiver(command, menuController, "MainMenu");
    }

    private void RouteToActiveReceiver(string command, MonoBehaviour receiver, string receiverName)
    {
        if (receiver == null || !receiver.isActiveAndEnabled)
        {
            Debug.Log($"[MenuVoiceBlocked] command={command} reason=NoActiveReceiver target={receiverName}");
            return;
        }

        Debug.Log($"[MenuVoiceRoute] command={command} target={receiverName}");
        SendCommandTo(receiver, command);
    }

    private static bool IsPanelActive(GameObject panel)
    {
        return panel != null && panel.activeInHierarchy;
    }

    private void ExecuteFallbackCommand(string command)
    {
        switch (command)
        {
            case CommandUp:
            case CommandDown:
            case CommandLeft:
            case CommandRight:
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
            Debug.Log($"[MenuVoiceTrace] SendCommandTo skipped. command={command}, target=null");
            return;
        }

        Debug.Log($"[MenuVoiceTrace] SendCommandTo before. command={command}, target={target.name}, type={target.GetType().Name}, scene={target.gameObject.scene.name}, activeSelf={target.gameObject.activeSelf}, activeInHierarchy={target.gameObject.activeInHierarchy}, enabled={target.enabled}");
        target.SendMessage("OnVoiceCommand", command, SendMessageOptions.DontRequireReceiver);
        Debug.Log($"[MenuVoiceTrace] SendCommandTo after. command={command}, target={target.name}");
    }

    private void CreateDebugMenu()
    {
        RuntimeDebug.DebugWindow window = RuntimeDebug.DebugMenu.CreateWindow(DebugWindowName);
        window.CreateButton("\u30bf\u30a4\u30e0", DebugExecuteTimeCommand);
        window.CreateButton("\u4e0a", DebugExecuteUpCommand);
        window.CreateButton("\u4e0b", DebugExecuteDownCommand);
        window.CreateButton("\u5de6", DebugExecuteLeftCommand);
        window.CreateButton("\u53f3", DebugExecuteRightCommand);
        window.CreateButton("\u6c7a\u5b9a", DebugExecuteDecisionCommand);
    }
}
