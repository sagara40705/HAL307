using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using static Sound.Manager.SoundManager;

public class VoiceMainMenuController : MonoBehaviour
{
    [Header("Menu")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private bool pauseGameWhenOpen = true;

    [Header("Cursor")]
    [Tooltip("動かしたいカーソル画像。例：ダンベルカーソル")]
    [SerializeField] private RectTransform cursor;

    [Tooltip("カーソルを項目の左側にずらす量")]
    [SerializeField] private Vector2 cursorOffset = new Vector2(-360f, 0f);

    [Header("Select Lights")]
    [Tooltip("各項目の光る画像。選択中の項目だけONになります")]
    [SerializeField] private GameObject[] selectLights;

    [Header("Menu Items")]
    [SerializeField] private RectTransform[] menuItems;

    [Header("Actions")]
    [SerializeField] private UnityEvent[] onDecideEvents;

    [Header("Debug Key Input")]
    [SerializeField] private bool enableDebugKeyInput = true;
    [SerializeField] private Key openMenuKey = Key.T;
    [SerializeField] private Key closeMenuKey = Key.Escape;
    [SerializeField] private Key upKey = Key.UpArrow;
    [SerializeField] private Key upKey2 = Key.W;
    [SerializeField] private Key downKey = Key.DownArrow;
    [SerializeField] private Key downKey2 = Key.S;
    [SerializeField] private Key decideKey = Key.Enter;
    [SerializeField] private Key decideKey2 = Key.Space;

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
        UpdateSelection();
    }

    private void Update()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (enableDebugKeyInput && WasPressed(openMenuKey))
        {
            OpenMenu();
        }

        if (enableDebugKeyInput && WasPressed(closeMenuKey))
        {
            CloseMenu();
        }

        if (requireOpenBeforeControl && !IsMenuControlAvailable())
        {
            return;
        }

        bool moveUp = Keyboard.current.upArrowKey.wasPressedThisFrame;
        bool moveDown = Keyboard.current.downArrowKey.wasPressedThisFrame;

        if (enableDebugKeyInput)
        {
            moveUp |= WasPressed(upKey) || WasPressed(upKey2);
            moveDown |= WasPressed(downKey) || WasPressed(downKey2);
        }

        if (moveUp)
        {
            MoveUp();
        }
        else if (moveDown)
        {
            MoveDown();
        }

        if (enableDebugKeyInput && (WasPressed(decideKey) || WasPressed(decideKey2)))
        {
            Decide();
        }
    }

    private bool IsMenuControlAvailable()
    {
        return menuPanel != null ? menuPanel.activeInHierarchy : isMenuOpen;
    }

    private bool WasPressed(Key key)
    {
        if (Keyboard.current == null)
        {
            return false;
        }

        if (!System.Enum.IsDefined(typeof(Key), key))
        {
            return false;
        }

        KeyControl keyControl = Keyboard.current[key];
        return keyControl != null && keyControl.wasPressedThisFrame;
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
//                    Sound.Manager.SoundManager.Play((int)SceneSoundTag.InGameSound, (int)InGameSoundTag.MoveCursorSE);
                }
                break;

            case "sita":
                if (!requireOpenBeforeControl || isMenuOpen)
                {
                    MoveDown();
//                    Sound.Manager.SoundManager.Play((int)SceneSoundTag.InGameSound, (int)InGameSoundTag.MoveCursorSE);
                }
                break;

            case "kettei":
                if (!requireOpenBeforeControl || isMenuOpen)
                {
                    Decide();
//                    Sound.Manager.SoundManager.Play((int)SceneSoundTag.InGameSound, (int)InGameSoundTag.DecideSE);
                }
                break;
        }
    }

    public void OpenMenu()
    {
        if (TryRouteOpenToPauseManager())
        {
            return;
        }

        Debug.Log("OpenMenuが呼ばれました");

        if (menuPanel != null)
        {
            menuPanel.SetActive(true);
        }

        isMenuOpen = true;

        currentIndex = 0;
        UpdateSelection();

        if (pauseGameWhenOpen)
        {
            Time.timeScale = 0f;
            Debug.Log("TimeScale = " + Time.timeScale);
        }

        Debug.Log("[VoiceMenuController] メニューを開きました");
    }

    public void CloseMenu()
    {
        if (TryRouteCloseToPauseManager())
        {
            return;
        }

        if (menuPanel != null)
        {
            menuPanel.SetActive(false);
        }

        isMenuOpen = false;

        if (cursor != null)
        {
            cursor.gameObject.SetActive(false);
        }

        SetAllLights(false);

        if (pauseGameWhenOpen)
        {
            Time.timeScale = 1f;
        }

        Debug.Log("[VoiceMenuController] メニューを閉じました");
    }

    private bool TryRouteOpenToPauseManager()
    {
        PauseManager pauseManager = PauseManager.Instance;
        if (pauseManager != null && pauseManager.IsApplyingPauseView)
        {
            return false;
        }

        GamePhaseManager gamePhaseManager = GamePhaseManager.Instance;
        if (gamePhaseManager != null && gamePhaseManager.IsInitialized && !gamePhaseManager.IsChangingPhase && gamePhaseManager.CurrentPhase != GamePhase.Pause)
        {
            gamePhaseManager.ChangePhase(GamePhase.Pause);
            return true;
        }

        if (pauseManager == null || !pauseManager.IsInitialized || pauseManager.IsApplyingPauseView || pauseManager.IsPaused)
        {
            return false;
        }

        pauseManager.OpenPause();
        return true;
    }

    private bool TryRouteCloseToPauseManager()
    {
        PauseManager pauseManager = PauseManager.Instance;
        if (pauseManager != null && pauseManager.IsApplyingPauseView)
        {
            return false;
        }

        GamePhaseManager gamePhaseManager = GamePhaseManager.Instance;
        if (gamePhaseManager != null && gamePhaseManager.IsInitialized && !gamePhaseManager.IsChangingPhase && gamePhaseManager.CurrentPhase == GamePhase.Pause)
        {
            gamePhaseManager.ResumePreviousPhase();
            return true;
        }

        if (pauseManager == null || !pauseManager.IsInitialized || pauseManager.IsApplyingPauseView || !pauseManager.IsPaused)
        {
            return false;
        }

        pauseManager.ClosePause();
        return true;
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

        UpdateSelection();
        Debug.Log("[VoiceMenuController] 上へ移動: " + currentIndex);

        Sound.Manager.SoundManager.Play((int)SceneSoundTag.InGameSound, (int)InGameSoundTag.MoveCursorSE);
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

        UpdateSelection();
        Debug.Log("[VoiceMenuController] 下へ移動: " + currentIndex);

        Sound.Manager.SoundManager.Play((int)SceneSoundTag.InGameSound, (int)InGameSoundTag.MoveCursorSE);
    }

    public void Decide()
    {
        string itemName = "None";

        if (menuItems != null &&
            currentIndex >= 0 &&
            currentIndex < menuItems.Length &&
            menuItems[currentIndex] != null)
        {
            itemName = menuItems[currentIndex].name;
        }

        Debug.Log($"[VoiceMenuController] 決定 index={currentIndex} item={itemName}");

        if (onDecideEvents == null)
        {
            return;
        }

        if (currentIndex < 0 || currentIndex >= onDecideEvents.Length)
        {
            return;
        }

        onDecideEvents[currentIndex]?.Invoke();

        Sound.Manager.SoundManager.Play((int)SceneSoundTag.InGameSound, (int)InGameSoundTag.DecideSE);
    }

    private void UpdateSelection()
    {
        if (menuItems == null || menuItems.Length == 0)
        {
            SetAllLights(false);

            if (cursor != null)
            {
                cursor.gameObject.SetActive(false);
            }

            return;
        }

        RectTransform target = menuItems[currentIndex];

        // カーソルを動かす
        if (cursor != null && target != null)
        {
            cursor.gameObject.SetActive(isMenuOpen || menuPanel == null || menuPanel.activeSelf);
            cursor.position = target.position + (Vector3)cursorOffset;
        }

        // 光る画像は動かさず、選択中のものだけ表示する
        UpdateLights();
    }

    private void UpdateLights()
    {
        if (selectLights == null)
        {
            return;
        }

        for (int i = 0; i < selectLights.Length; i++)
        {
            if (selectLights[i] == null)
            {
                continue;
            }

            bool selected = i == currentIndex;
            bool show = selected && (isMenuOpen || menuPanel == null || menuPanel.activeSelf);

            selectLights[i].SetActive(show);
        }
    }

    private void SetAllLights(bool active)
    {
        if (selectLights == null)
        {
            return;
        }

        for (int i = 0; i < selectLights.Length; i++)
        {
            if (selectLights[i] != null)
            {
                selectLights[i].SetActive(active);
            }
        }
    }

    public void ResetCursor()
    {
        currentIndex = 0;
        UpdateSelection();
    }

    public void RefreshSelection()
    {
        UpdateSelection();
    }
}
