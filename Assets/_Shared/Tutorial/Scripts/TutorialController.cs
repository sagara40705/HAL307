using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class TutorialController : MonoBehaviour
{
    [Header("スライド対象")]
    [SerializeField] private RectTransform contentRoot;

    [Header("ページ設定")]
    [SerializeField] private float pageWidth = 1920f;
    [SerializeField] private float slideDuration = 0.3f;

    [Header("ページ番号表示")]
    [SerializeField] private TextMeshProUGUI pageText;

    [Header("モデル制御")]
    [SerializeField] private TutorialModelController modelController;

    private int currentPage = 0;
    private bool isSliding = false;

    private void Start()
    {
        UpdatePageText();

        if (modelController != null)
        {
            modelController.PlayByPage(currentPage);
        }
    }

    private void Update()
    {
        if (isSliding) return;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.rightArrowKey.wasPressedThisFrame ||
                Keyboard.current.dKey.wasPressedThisFrame)
            {
                NextPage();
            }

            if (Keyboard.current.leftArrowKey.wasPressedThisFrame ||
                Keyboard.current.aKey.wasPressedThisFrame)
            {
                PrevPage();
            }
        }

        if (Gamepad.current != null)
        {
            if (Gamepad.current.dpad.right.wasPressedThisFrame ||
                Gamepad.current.leftStick.right.wasPressedThisFrame)
            {
                NextPage();
            }

            if (Gamepad.current.dpad.left.wasPressedThisFrame ||
                Gamepad.current.leftStick.left.wasPressedThisFrame)
            {
                PrevPage();
            }
        }
    }

    private void NextPage()
    {
        int targetPage = currentPage + 1;

        if (targetPage >= contentRoot.childCount)
            return;

        SlideToPage(targetPage).Forget();
    }

    private void PrevPage()
    {
        int targetPage = currentPage - 1;

        if (targetPage < 0)
            return;

        SlideToPage(targetPage).Forget();
    }

    private async UniTask SlideToPage(int targetPage)
    {
        isSliding = true;

        Vector2 startPos = contentRoot.anchoredPosition;

        Vector2 endPos = new Vector2(
            -pageWidth * targetPage,
            startPos.y
        );

        float time = 0f;

        while (time < slideDuration)
        {
            time += Time.deltaTime;

            float t = Mathf.SmoothStep(
                0f,
                1f,
                time / slideDuration
            );

            contentRoot.anchoredPosition =
                Vector2.Lerp(startPos, endPos, t);

            await UniTask.Yield();
        }

        contentRoot.anchoredPosition = endPos;

        currentPage = targetPage;

        UpdatePageText();

        if (modelController != null)
        {
            modelController.PlayByPage(currentPage);
        }

        isSliding = false;
    }

    private void UpdatePageText()
    {
        if (pageText == null) return;

        pageText.text =
            $"{currentPage + 1} / {contentRoot.childCount}";
    }
}
