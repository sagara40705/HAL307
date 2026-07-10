using UnityEngine;

public class MenuVoiceCommandAction : MonoBehaviour
{
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

    private void EnableMenuControl()
    {
        menuControlEnabled = true;
        menuControlTimer = menuControlEnableTime;

        Debug.Log("[Menu Voice] time認識。メニュー操作を有効にしました。");

        if (PauseManager.Instance != null)
        {
            PauseManager.Instance.OpenPause();
        }
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

        if (titleSceneMgr != null)
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
}
