using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class HowToController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject howToPanel;

    [Header("Images")]
    [SerializeField] private Image howToImage;
    [SerializeField] private Sprite[] pages;

    [Header("Cursor")]
    [SerializeField] private RectTransform cursor;
    [SerializeField] private RectTransform[] menuItems;
    [SerializeField] private Vector2 cursorOffset = new Vector2(-260f, 0f);

    [Header("Input")]
    [SerializeField] private Key upKey = Key.UpArrow;
    [SerializeField] private Key downKey = Key.DownArrow;
    [SerializeField] private Key leftKey = Key.LeftArrow;
    [SerializeField] private Key rightKey = Key.RightArrow;
    [SerializeField] private Key decideKey = Key.Enter;
    [SerializeField] private Key decideKey2 = Key.Space;

    private int currentIndex;
    private int pageIndex;

    private void Start()
    {
        if (howToPanel != null)
        {
            howToPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (howToPanel == null || !howToPanel.activeSelf)
        {
            return;
        }

        HandleKeyboardInput();
    }

    public void OpenHowTo()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }

        if (howToPanel != null)
        {
            howToPanel.SetActive(true);
        }

        currentIndex = 0;
        pageIndex = 0;

        UpdatePage();
        UpdateCursor();

        Debug.Log("操作説明を開きました");
    }

    public void CloseHowTo()
    {
        if (howToPanel != null)
        {
            howToPanel.SetActive(false);
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }

        Debug.Log("操作説明を閉じました");
    }

    public void OnVoiceCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return;
        }

        command = command.Trim().ToLowerInvariant();

        switch (command)
        {
            case "ue":
                MoveUp();
                break;

            case "sita":
                MoveDown();
                break;

            case "kettei":
                Decide();
                break;
        }
    }

    private void HandleKeyboardInput()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (WasPressed(upKey))
        {
            MoveUp();
        }

        if (WasPressed(downKey))
        {
            MoveDown();
        }

        if (WasPressed(leftKey))
        {
            PrevPage();
        }

        if (WasPressed(rightKey))
        {
            NextPage();
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

        return Keyboard.current[key].wasPressedThisFrame;
    }

    private void MoveUp()
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

        UpdateCursor();
    }

    private void MoveDown()
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

        UpdateCursor();
    }

    private void Decide()
    {
        // 戻るボタンが2番目なら currentIndex == 1
        if (currentIndex == 1)
        {
            CloseHowTo();
        }
    }

    private void PrevPage()
    {
        if (pages == null || pages.Length == 0)
        {
            return;
        }

        pageIndex--;

        if (pageIndex < 0)
        {
            pageIndex = pages.Length - 1;
        }

        UpdatePage();
    }

    private void NextPage()
    {
        if (pages == null || pages.Length == 0)
        {
            return;
        }

        pageIndex++;

        if (pageIndex >= pages.Length)
        {
            pageIndex = 0;
        }

        UpdatePage();
    }

    private void UpdatePage()
    {
        if (howToImage == null || pages == null || pages.Length == 0)
        {
            return;
        }

        howToImage.sprite = pages[pageIndex];
    }

    private void UpdateCursor()
    {
        if (cursor == null || menuItems == null || menuItems.Length == 0)
        {
            return;
        }

        RectTransform target = menuItems[currentIndex];

        if (target == null)
        {
            return;
        }

        cursor.gameObject.SetActive(true);
        cursor.position = target.position + (Vector3)cursorOffset;
    }
}
