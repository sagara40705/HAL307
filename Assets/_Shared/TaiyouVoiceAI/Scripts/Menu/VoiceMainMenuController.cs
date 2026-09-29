using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

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
    [SerializeField] private Key leftKey = Key.LeftArrow;
    [SerializeField] private Key rightKey = Key.RightArrow;
    [SerializeField] private Key decideKey = Key.Enter;
    [SerializeField] private Key decideKey2 = Key.Space;

    [Header("Left / Right Actions")]
    [SerializeField] private UnityEvent[] onLeftEvents;
    [SerializeField] private UnityEvent[] onRightEvents;

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
        if (!enableDebugKeyInput)
        {
            return;
        }

        if (Keyboard.current == null)
        {
            return;
        }

        if (WasPressed(openMenuKey))
        {
            OpenMenu();
        }

        if (WasPressed(closeMenuKey))
        {
            CloseMenu();
        }

        if (requireOpenBeforeControl && !isMenuOpen)
        {
            return;
        }

        if (WasPressed(upKey) || WasPressed(upKey2))
        {
            MoveUp();
        }

        if (WasPressed(downKey) || WasPressed(downKey2))
        {
            MoveDown();
        }

        if (WasPressed(leftKey))
        {
            MoveLeft();
        }

        if (WasPressed(rightKey))
        {
            MoveRight();
        }

        if (WasPressed(decideKey) || WasPressed(decideKey2))
        {
            Decide();
        }
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
        Debug.Log($"[MenuVoiceTrace] VoiceMainMenuController.OnVoiceCommand entered. command={command}, isMenuOpen={isMenuOpen}, requireOpenBeforeControl={requireOpenBeforeControl}, object={name}, scene={gameObject.scene.name}, activeSelf={gameObject.activeSelf}, activeInHierarchy={gameObject.activeInHierarchy}, enabled={enabled}");

        if (string.IsNullOrWhiteSpace(command))
        {
            Debug.Log("[MenuVoiceTrace] VoiceMainMenuController.OnVoiceCommand return. reason=empty command");
            return;
        }

        command = command.Trim().ToLowerInvariant();
        Debug.Log($"[MenuVoiceTrace] VoiceMainMenuController.OnVoiceCommand normalized command={command}");

        switch (command)
        {
            case "time":
                Debug.Log("[MenuVoiceTrace] VoiceMainMenuController command=time -> OpenMenu");
                OpenMenu();
                break;

            case "ue":
                if (!requireOpenBeforeControl || isMenuOpen)
                {
                    Debug.Log("[MenuVoiceTrace] VoiceMainMenuController command=ue -> MoveUp");
                    MoveUp();
                }
                else
                {
                    Debug.Log("[MenuVoiceTrace] VoiceMainMenuController command=ue skipped. reason=menu not open");
                }
                break;

            case "sita":
                if (!requireOpenBeforeControl || isMenuOpen)
                {
                    Debug.Log("[MenuVoiceTrace] VoiceMainMenuController command=sita -> MoveDown");
                    MoveDown();
                }
                else
                {
                    Debug.Log("[MenuVoiceTrace] VoiceMainMenuController command=sita skipped. reason=menu not open");
                }
                break;

            case "hidari":
                if (!requireOpenBeforeControl || isMenuOpen)
                {
                    Debug.Log("[Menu Voice] hidari -> MoveLeft");
                    Debug.Log("[MenuVoiceTrace] VoiceMainMenuController command=hidari -> MoveLeft");
                    MoveLeft();
                }
                else
                {
                    Debug.Log("[MenuVoiceTrace] VoiceMainMenuController command=hidari skipped. reason=menu not open");
                }
                break;

            case "migi":
                if (!requireOpenBeforeControl || isMenuOpen)
                {
                    Debug.Log("[Menu Voice] migi -> MoveRight");
                    Debug.Log("[MenuVoiceTrace] VoiceMainMenuController command=migi -> MoveRight");
                    MoveRight();
                }
                else
                {
                    Debug.Log("[MenuVoiceTrace] VoiceMainMenuController command=migi skipped. reason=menu not open");
                }
                break;

            case "kettei":
                if (!requireOpenBeforeControl || isMenuOpen)
                {
                    Debug.Log("[MenuVoiceTrace] VoiceMainMenuController command=kettei -> Decide");
                    Decide();
                }
                else
                {
                    Debug.Log("[MenuVoiceTrace] VoiceMainMenuController command=kettei skipped. reason=menu not open");
                }
                break;

            default:
                Debug.Log($"[MenuVoiceTrace] VoiceMainMenuController command unsupported. command={command}");
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
    }

    public void MoveLeft()
    {
        if (!TryInvokeDirectionalEvent(onLeftEvents, "left"))
        {
            Debug.Log($"[VoiceMenuController] 左操作は現在の項目では未設定です: {currentIndex}");
        }
    }

    public void MoveRight()
    {
        if (!TryInvokeDirectionalEvent(onRightEvents, "right"))
        {
            Debug.Log($"[VoiceMenuController] 右操作は現在の項目では未設定です: {currentIndex}");
        }
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
    }

    private bool TryInvokeDirectionalEvent(UnityEvent[] events, string direction)
    {
        if (events == null || currentIndex < 0 || currentIndex >= events.Length || events[currentIndex] == null)
        {
            Debug.Log($"[MenuVoiceTrace] Directional event skipped. direction={direction}, index={currentIndex}, reason=not assigned");
            return false;
        }

        Debug.Log($"[MenuVoiceTrace] Directional event invoked. direction={direction}, index={currentIndex}");
        events[currentIndex].Invoke();
        return true;
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
