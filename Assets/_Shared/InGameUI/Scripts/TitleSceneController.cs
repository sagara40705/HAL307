using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;


public class TitleSceneController : MonoBehaviour
{
    [Header("メニューの項目")]
    public RectTransform menuItems;

    [Header("選択アイコン")]
    public RectTransform iconLeft;
    public RectTransform iconRight;
    public float iconOffsetX = 50f;

    [Header("選択中の色")]
    public Color selectedColor = Color.yellow;
    public Color normalColor = Color.white;

    private int currentIndex = 0;

    void Start()
    {
        UpdateMenuVisuals();
    }

    void Update()
    {
        // 
        bool upPressed = false;
        bool downPressed = false;
        bool submitPressed = false;

        // キーボードの判定
        if (Keyboard.current != null)
        {
            upPressed |= Keyboard.current.upArrowKey.wasPressedThisFrame || Keyboard.current.wKey.wasPressedThisFrame;
            downPressed |= Keyboard.current.downArrowKey.wasPressedThisFrame || Keyboard.current.sKey.wasPressedThisFrame;
            submitPressed |= Keyboard.current.enterKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame;
        }

        // ゲームパッドの判定
        if (Gamepad.current != null)
        {
            upPressed |= Gamepad.current.dpad.up.wasPressedThisFrame || Gamepad.current.leftStick.up.wasPressedThisFrame;
            downPressed |= Gamepad.current.dpad.down.wasPressedThisFrame || Gamepad.current.leftStick.down.wasPressedThisFrame;
            submitPressed |= Gamepad.current.buttonSouth.wasPressedThisFrame; // PSなら×/〇、XboxならAボタン
        }

        // 入力処理の実行
        if (upPressed)
        {
            currentIndex = (currentIndex - 1 + menuItems.childCount) % menuItems.childCount;
            UpdateMenuVisuals();
        }
        else if (downPressed)
        {
            currentIndex = (currentIndex + 1) % menuItems.childCount;
            UpdateMenuVisuals();
        }

        if (submitPressed)
        {
            ExecuteSelection();
        }
    }

    void UpdateMenuVisuals()
    {
        for (int i = 0; i < menuItems.childCount; i++)
        {
            var text = menuItems.GetChild(i).GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                text.color = (i == currentIndex) ? selectedColor : normalColor;
            }

            if (i == currentIndex)
            {
                if (iconLeft != null)
                {
                    iconLeft.SetParent(menuItems.GetChild(i), false);
                    iconLeft.anchoredPosition = new Vector2(-iconOffsetX, 10);
                }
                if (iconRight != null)
                {
                    iconRight.SetParent(menuItems.GetChild(i), false);
                    iconRight.anchoredPosition = new Vector2(iconOffsetX, 10);
                }
            }

            menuItems.GetChild(i).localScale = (i == currentIndex) ? Vector3.one * 1.2f : Vector3.one;
        }
    }

    void ExecuteSelection()
    {
        var selectedItem = menuItems.GetChild(currentIndex).name;

        switch (selectedItem)
        {
            case "Start":
                TransitionManager.Instance.StartDumbbellTransition(SceneController.Instance.gameSceneName).Forget();
                break;
            case "Exit":
                Application.Quit();
                break;
            default:
                Debug.LogWarning("未定義のメニュー項目が選択されました: " + selectedItem);
                break;
        }
    }
}