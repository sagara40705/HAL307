using UnityEngine;

public class MenuVoiceCommandAction : MonoBehaviour
{
    private const string DebugWindowName = "Voice Command";

    [Header("Title")]
    [SerializeField] private TitleSceneMgr titleSceneMgr;

    [Header("Pause Menu / Menu Controller")]
    [Tooltip("VoiceMenuControllerなど、OnVoiceCommand(string command) を持つスクリプトを入れてください。")]
    [SerializeField] private MonoBehaviour menuController;

    [Header("Stage Select")]
    [Tooltip("StageSelectControllerなど、OnVoiceCommand(string command) を持つスクリプトを入れてください。")]
    [SerializeField] private MonoBehaviour stageSelectController;

    [Header("Time Command")]
    [SerializeField] private bool requireTimeBeforeMenuControl = true;
    [SerializeField] private float menuControlEnableTime = 5.0f;



    private bool menuControlEnabled;
    private float menuControlTimer;

    private void Start()
    {
        if (titleSceneMgr == null)
        {
            titleSceneMgr = FindFirstObjectByType<TitleSceneMgr>();
        }

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
            Debug.Log("[Menu Voice] time有効時間が終了しました。");
        }
    }

    public void ExecuteCommand(string command, float volume)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return;
        }

        command = command.Trim().ToLowerInvariant();

        if (command == "none")
        {
            return;
        }

        if (command == "time")
        {
            EnableMenuControl();
            return;
        }

        if (requireTimeBeforeMenuControl && !menuControlEnabled)
        {
            Debug.Log($"[Menu Voice] {command} は time の後だけ有効です。");
            return;
        }

        switch (command)
        {
            case "ue":
                OnUp();
                break;

            case "sita":
                OnDown();
                break;

            case "kettei":
                OnDecision();
                break;

            default:
                Debug.Log($"[Menu Voice] 未対応コマンド: {command}");
                break;
        }
    }

    public void DebugExecuteTimeCommand()
    {
        ExecuteCommand("time", 0f);
    }

    public void DebugExecuteUpCommand()
    {
        ExecuteCommand("ue", 0f);
    }

    public void DebugExecuteDownCommand()
    {
        ExecuteCommand("sita", 0f);
    }

    public void DebugExecuteDecisionCommand()
    {
        ExecuteCommand("kettei", 0f);
    }

    private void EnableMenuControl()
    {
        menuControlEnabled = true;
        menuControlTimer = menuControlEnableTime;

        Debug.Log("[Menu Voice] time認識。メニュー操作を有効にしました。");

        PauseManager pauseManager = PauseManager.Instance;
        if (pauseManager != null && pauseManager.IsInitialized)
        {
            pauseManager.OpenPause();
            return;
        }

        SendCommandTo(menuController, "time");
    }

    private void OnUp()
    {
        Debug.Log("[Menu Voice] 上");
        SendCommandTo(menuController, "ue");
        SendCommandTo(stageSelectController, "ue");
    }

    private void OnDown()
    {
        Debug.Log("[Menu Voice] 下");
        SendCommandTo(menuController, "sita");
        SendCommandTo(stageSelectController, "sita");
    }

    private void OnDecision()
    {
        Debug.Log("[Menu Voice] 決定");

        PauseManager pauseManager = PauseManager.Instance;
        if (titleSceneMgr != null && (pauseManager == null || !pauseManager.IsPaused))
        {
            titleSceneMgr.OnVoiceCommand("kettei");
        }

        SendCommandTo(menuController, "kettei");
        SendCommandTo(stageSelectController, "kettei");
    }

    private void SendCommandTo(MonoBehaviour target, string command)
    {
        if (target == null)
        {
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
