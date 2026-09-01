using UnityEngine;
using UnityEngine.Events;
using static Sound.Manager.SoundManager;

public class VoiceTitleMenuController : MonoBehaviour
{
    [Header("Menu")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private bool pauseGameWhenOpen = true;

    [Header("Select Light")]
    [SerializeField] private RectTransform selectLight;

    [Header("Menu Items")]
    [SerializeField] private RectTransform[] menuItems;

    [Header("Actions")]
    [SerializeField] private UnityEvent[] onDecideEvents;

    [Header("Debug Key Input")]
    [SerializeField] private bool enableDebugKeyInput = true;
    [SerializeField] private UnityEngine.InputSystem.Key openMenuKey = UnityEngine.InputSystem.Key.T;
    [SerializeField] private UnityEngine.InputSystem.Key closeMenuKey = UnityEngine.InputSystem.Key.Escape;
    [SerializeField] private UnityEngine.InputSystem.Key upKey = UnityEngine.InputSystem.Key.UpArrow;
    [SerializeField] private UnityEngine.InputSystem.Key upKey2 = UnityEngine.InputSystem.Key.W;
    [SerializeField] private UnityEngine.InputSystem.Key downKey = UnityEngine.InputSystem.Key.DownArrow;
    [SerializeField] private UnityEngine.InputSystem.Key downKey2 = UnityEngine.InputSystem.Key.S;
    [SerializeField] private UnityEngine.InputSystem.Key decideKey = UnityEngine.InputSystem.Key.R;
    [SerializeField] private UnityEngine.InputSystem.Key decideKey2 = UnityEngine.InputSystem.Key.Space;

    [Header("Debug Time Rule")]
    [SerializeField] private bool requireOpenBeforeControl = true;

    private int currentIndex;
    private bool isMenuOpen;

    private void Start()
    {
        if (menuPanel != null)
        {
            isMenuOpen = menuPanel.activeSelf;
        }

        currentIndex = 0;
        UpdateSelectLight();
        Sound.Manager.SoundManager.Play((int)SceneSoundTag.TitleSound, (int)TitleSoundTag.TitleBGM);
    }

    private void Update()
    {
        if (!enableDebugKeyInput)
        {
            return;
        }

        // デバッグ用：Tで音声の「time」と同じ動き
        if (DebugShortcuts.Pressed(openMenuKey))
        {
            OpenMenu();
        }

        // デバッグ用：Escapeで閉じる
        if (DebugShortcuts.Pressed(closeMenuKey))
        {
            CloseMenu();
        }

        // requireOpenBeforeControl がONなら、メニューが開いている時だけ上下決定を受け付ける
        if (requireOpenBeforeControl && !isMenuOpen)
        {
            return;
        }

        if (DebugShortcuts.Pressed(upKey) || DebugShortcuts.Pressed(upKey2))
        {
            MoveUp();
        }

        if (DebugShortcuts.Pressed(downKey) || DebugShortcuts.Pressed(downKey2))
        {
            MoveDown();
        }

        if (DebugShortcuts.Pressed(decideKey) || DebugShortcuts.Pressed(decideKey2))
        {
            Decide();
        }
    }

    /// <summary>
    /// MenuVoiceCommandAction から呼ばれる。
    /// ue / sita / kettei / time に対応。
    /// </summary>
    public void OnVoiceCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return;
        }

        command = command.Trim().ToLowerInvariant();

        switch (command)
        {
            case "time":
                OpenMenu();
                break;

            case "ue":
                if (!requireOpenBeforeControl || isMenuOpen)
                {
                    MoveUp();
                }
                break;

            case "sita":
                if (!requireOpenBeforeControl || isMenuOpen)
                {
                    MoveDown();
                }
                break;

            case "kettei":
                if (!requireOpenBeforeControl || isMenuOpen)
                {
                    Decide();

                    GamePhaseManager gamePhaseManager = GamePhaseManager.Instance;
                    gamePhaseManager.ChangePhase(GamePhase.InGame);
                }
                break;
        }
    }

    public void OpenMenu()
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
        }

        isMenuOpen = true;
        currentIndex = 0;
        UpdateSelectLight();

        if (pauseGameWhenOpen)
        {
            Time.timeScale = 0f;
        }

        Debug.Log("[VoiceMenuController] メニューを開きました");
    }

    public void CloseMenu()
    {
        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }

        isMenuOpen = false;

        if (selectLight != null)
        {
            selectLight.gameObject.SetActive(false);
        }

        if (pauseGameWhenOpen)
        {
            Time.timeScale = 1f;
        }

        Debug.Log("[VoiceMenuController] メニューを閉じました");
    }

    public void MoveUp()
    {
        if (menuItems == null || menuItems.Length == 0)
        {
            return;
        }

        currentIndex--;

        if (currentIndex < 0)
        {
            currentIndex = menuItems.Length - 1;
        }

        UpdateSelectLight();
        Debug.Log("[VoiceMenuController] 上へ移動: " + currentIndex);
    }

    public void MoveDown()
    {
        if (menuItems == null || menuItems.Length == 0)
        {
            return;
        }

        currentIndex++;

        if (currentIndex >= menuItems.Length)
        {
            currentIndex = 0;
        }

        UpdateSelectLight();
        Debug.Log("[VoiceMenuController] 下へ移動: " + currentIndex);
    }

    public void Decide()
    {
        Debug.Log("[VoiceMenuController] 決定: " + currentIndex);

        if (onDecideEvents == null)
        {
            return;
        }

        if (currentIndex < 0 || currentIndex >= onDecideEvents.Length)
        {
            return;
        }

        onDecideEvents[currentIndex]?.Invoke();
    }

    private void UpdateSelectLight()
    {
        if (selectLight == null)
        {
            return;
        }

        if (menuItems == null || menuItems.Length == 0)
        {
            selectLight.gameObject.SetActive(false);
            return;
        }

        RectTransform target = menuItems[currentIndex];
        if (target == null)
        {
            selectLight.gameObject.SetActive(false);
            return;
        }

        selectLight.gameObject.SetActive(isMenuOpen || menuPanel == null || menuPanel.activeSelf);
        selectLight.position = target.position;
        selectLight.sizeDelta = target.sizeDelta;
    }
}
