using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class MicSettingController : MonoBehaviour
{
    [Header("Hold Adjust")]
    [SerializeField] private int sensitivityStep = 1;
    [SerializeField] private float firstRepeatDelay = 0.35f;
    [SerializeField] private float repeatInterval = 0.06f;

    private float leftHoldTimer;
    private float rightHoldTimer;
    private bool leftWasHeld;
    private bool rightWasHeld;


    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject micSettingPanel;

    [Header("References")]
    [SerializeField] private MicrophoneInput microphoneInput;
    [SerializeField] private VoiceOnnxRunner_Menu voiceRunner;

    [Header("UI")]
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private Slider volumeMeterSlider;
    [SerializeField] private TextMeshProUGUI sensitivityText;

    [Header("Cursor")]
    [SerializeField] private RectTransform cursor;
    [SerializeField] private RectTransform[] menuItems;
    [SerializeField] private Vector2 cursorOffset = new Vector2(-260f, 0f);

    [Header("Sensitivity")]
    [SerializeField, Range(0, 100)]
    private int sensitivity = 70;

    [SerializeField] private float minThreshold = 0.005f;
    [SerializeField] private float maxThreshold = 0.08f;

    [Header("Input")]
    [SerializeField] private Key upKey = Key.UpArrow;
    [SerializeField] private Key downKey = Key.DownArrow;
    [SerializeField] private Key leftKey = Key.LeftArrow;
    [SerializeField] private Key rightKey = Key.RightArrow;
    [SerializeField] private Key decideKey = Key.Enter;
    [SerializeField] private Key decideKey2 = Key.Space;

    private int currentIndex;

    private void Start()
    {
        ApplySensitivity();
        UpdateCursor();

        if (micSettingPanel != null)
        {
            micSettingPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (micSettingPanel == null || !micSettingPanel.activeSelf)
        {
            return;
        }

        UpdateVolumeMeter();
        HandleKeyboardInput();
    }

    public void OpenMicSetting()
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }

        if (micSettingPanel != null)
        {
            micSettingPanel.SetActive(true);
        }

        currentIndex = 0;
        UpdateCursor();

        Debug.Log("マイク設定を開きました");
    }

    public void CloseMicSetting()
    {
        if (micSettingPanel != null)
        {
            micSettingPanel.SetActive(false);
        }

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }

        Debug.Log("マイク設定を閉じました");
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

        HandleSensitivityHoldInput();

        if (WasPressed(decideKey) || WasPressed(decideKey2))
        {
            Decide();
        }
    }

    private bool WasPressed(Key key)
    {
        if (!System.Enum.IsDefined(typeof(Key), key))
        {
            return false;
        }

        return Keyboard.current[key].wasPressedThisFrame;
    }

    private void MoveUp()
    {
        currentIndex--;

        if (currentIndex < 0)
        {
            currentIndex = menuItems.Length - 1;
        }

        UpdateCursor();
    }

    private void MoveDown()
    {
        currentIndex++;

        if (currentIndex >= menuItems.Length)
        {
            currentIndex = 0;
        }

        UpdateCursor();
    }

    private void Decide()
    {
        if (currentIndex == 0)
        {
            ChangeSensitivity(5);
        }
        else if (currentIndex == 1)
        {
            CloseMicSetting();
        }
    }

    private void ChangeSensitivity(int value)
    {
        sensitivity += value;
        sensitivity = Mathf.Clamp(sensitivity, 0, 100);

        ApplySensitivity();
    }

    private void ApplySensitivity()
    {
        if (sensitivitySlider != null)
        {
            sensitivitySlider.value = sensitivity;
        }

        if (sensitivityText != null)
        {
            sensitivityText.text = sensitivity.ToString();
        }

        // 感度が高いほど、しきい値を低くする
        float t = sensitivity / 100f;
        float threshold = Mathf.Lerp(maxThreshold, minThreshold, t);

        if (voiceRunner != null)
        {
            voiceRunner.SetMinVolumeThreshold(threshold);
        }

        Debug.Log($"マイク感度: {sensitivity} / threshold: {threshold:F4}");
    }

    private void UpdateVolumeMeter()
    {
        if (microphoneInput == null || volumeMeterSlider == null)
        {
            return;
        }

        float[] samples = microphoneInput.GetLatestSamples();

        if (samples == null || samples.Length == 0)
        {
            volumeMeterSlider.value = 0;
            return;
        }

        float max = 0f;

        for (int i = 0; i < samples.Length; i++)
        {
            float abs = Mathf.Abs(samples[i]);
            if (abs > max)
            {
                max = abs;
            }
        }

        volumeMeterSlider.value = Mathf.Clamp01(max);
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

    private void HandleSensitivityHoldInput()
    {
        bool leftHeld = IsHeld(leftKey);
        bool rightHeld = IsHeld(rightKey);

        HandleHoldAdjust(leftHeld, ref leftWasHeld, ref leftHoldTimer, -sensitivityStep);
        HandleHoldAdjust(rightHeld, ref rightWasHeld, ref rightHoldTimer, sensitivityStep);
    }

    private void HandleHoldAdjust(bool isHeld, ref bool wasHeld, ref float timer, int amount)
    {
        if (!isHeld)
        {
            wasHeld = false;
            timer = 0f;
            return;
        }

        // 押した瞬間に1回変更
        if (!wasHeld)
        {
            ChangeSensitivity(amount);
            wasHeld = true;
            timer = firstRepeatDelay;
            return;
        }

        // 長押し中は一定間隔で変更
        timer -= Time.unscaledDeltaTime;

        if (timer <= 0f)
        {
            ChangeSensitivity(amount);
            timer = repeatInterval;
        }
    }

    private bool IsHeld(Key key)
    {
        if (Keyboard.current == null)
        {
            return false;
        }

        if (!System.Enum.IsDefined(typeof(Key), key))
        {
            return false;
        }

        return Keyboard.current[key].isPressed;
    }
}
