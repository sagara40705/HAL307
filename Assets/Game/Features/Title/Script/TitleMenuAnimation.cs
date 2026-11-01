using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static Sound.Manager.SoundManager;

public enum TitleMenuAnimationState
{
    Loading,
    Selecting,
    PreparingConfirmation,
    Confirming,
    Transition
}

// 既存のTitleUiManagerとTitleSceneMgrに演出を追加する。
// 選択とシーン遷移は、引き続き既存の実装を基準とする。
public sealed class TitleMenuAnimation : MonoBehaviour
{
    private const float FramesPerSecond = 60f;
    private const int PreviewFrameCount = 41;
    private const int ConfirmFrameCount = 60;
    private const string GrandPrixPath = "TitleUI/Select_GrandPrix/Select_Grand-Prix_";
    private const string EndlessPath = "TitleUI/Select_Endless/Select_Endless_";

    [SerializeField] private RawImage _image;
    [SerializeField] private TitleUiManager _menu;
    [SerializeField] private RectTransform _cursor;
    [SerializeField] private RectTransform[] _menuItems;
    [SerializeField] private UnityEvent _onConfirmed = new UnityEvent();

    public static TitleMenuAnimation Instance { get; private set; }
    public TitleMenuAnimationState State => _state;
    public int SelectIndex => _selectIndex;
    public int CurrentFrame => _currentFrame;

    private readonly Texture2D[][] _frames =
    {
        new Texture2D[ConfirmFrameCount], new Texture2D[ConfirmFrameCount]
    };
    private TitleMenuAnimationState _state;
    private int _selectIndex;
    private int _currentFrame;
    private float _elapsedTime;
    private bool _previewsReady;
    private bool _confirmationRequested;
    private bool _menuWasEnabled;

    private void Awake()
    {
        if (_image == null || _menu == null || _cursor == null || _menuItems == null ||
            _menuItems.Length != 2 || _menuItems[0] == null || _menuItems[1] == null ||
            _onConfirmed.GetPersistentEventCount() == 0)
        {
            Debug.LogError("[TitleMenuAnimation] Title scene references are incomplete.");
            enabled = false;
            return;
        }

        Instance = this;
        _frames[0][0] = Resources.Load<Texture2D>(GrandPrixPath + "00000");
        _frames[1][0] = Resources.Load<Texture2D>(EndlessPath + "00000");
        ResetAnimation();
    }

    private IEnumerator Start()
    {
        yield return LoadFrames(0, 1, PreviewFrameCount);
        yield return LoadFrames(1, 1, PreviewFrameCount);
        if (!FramesReady(0, PreviewFrameCount) || !FramesReady(1, PreviewFrameCount))
        {
            Debug.LogError("[TitleMenuAnimation] A selection sequence is incomplete.");
            RestoreSelection();
            yield break;
        }

        _previewsReady = true;
        if (_confirmationRequested)
        {
            yield return PrepareConfirmation();
        }
        else
        {
            _state = TitleMenuAnimationState.Selecting;
            ResetAnimation();
        }
    }

    private void Update()
    {
        UpdateAnimation(Time.unscaledDeltaTime);
    }

    private void UpdateAnimation(float deltaTime)
    {
        if (_state == TitleMenuAnimationState.Selecting)
        {
            _elapsedTime = (_elapsedTime + deltaTime) % (PreviewFrameCount / FramesPerSecond);
            DrawFrame(Mathf.Min(Mathf.FloorToInt(_elapsedTime * FramesPerSecond), PreviewFrameCount - 1));
        }
        else if (_state == TitleMenuAnimationState.Confirming)
        {
            _elapsedTime += deltaTime;
            DrawFrame(Mathf.Min(Mathf.FloorToInt(_elapsedTime * FramesPerSecond), ConfirmFrameCount - 1));
            if (_elapsedTime >= ConfirmFrameCount / FramesPerSecond)
            {
                _state = TitleMenuAnimationState.Transition;
                Sound.Manager.SoundManager.Stop((int)SceneSoundTag.TitleSound, (int)TitleSoundTag.TitleBGM);
                _onConfirmed.Invoke();
            }
        }
    }

    private void LateUpdate()
    {
        if (_confirmationRequested || (_state != TitleMenuAnimationState.Loading &&
            _state != TitleMenuAnimationState.Selecting))
        {
            return;
        }

        // 矢印キーは既存メニューのUpdateで処理する。その後にカーソルを確認し、
        // このコンポーネント側で別の選択状態を保持しないようにする。
        SyncSelection();
        Gamepad pad = Gamepad.current;
        if (DebugShortcuts.Pressed(Key.W) || (pad != null && pad.dpad.up.wasPressedThisFrame))
            MoveSelection(true);
        else if (DebugShortcuts.Pressed(Key.S) || (pad != null && pad.dpad.down.wasPressedThisFrame))
            MoveSelection(false);

        if (TryClickedMode(out int index))
        {
            if (index != _selectIndex) MoveSelection(index == 0);
            RequestConfirmation();
        }
        else if (DebugShortcuts.Pressed(Key.Space) || DebugShortcuts.Pressed(Key.Enter) ||
                 DebugShortcuts.Pressed(Key.NumpadEnter) || (pad != null && pad.buttonSouth.wasPressedThisFrame))
        {
            RequestConfirmation();
        }
    }

    public void OnVoiceCommand(string command)
    {
        if (!isActiveAndEnabled || _confirmationRequested || string.IsNullOrWhiteSpace(command)) return;
        switch (command.Trim().ToLowerInvariant())
        {
            case "ue": MoveSelection(true); break;
            case "sita": MoveSelection(false); break;
            case "kettei": SyncSelection(); RequestConfirmation(); break;
        }
    }

    private void MoveSelection(bool up)
    {
        if (up) _menu.UpCommand();
        else _menu.DownCommand();
        SyncSelection();
    }

    private void SyncSelection()
    {
        float y = _cursor.anchoredPosition.y;
        int index = Mathf.Abs(y - _menuItems[0].anchoredPosition.y) <=
                    Mathf.Abs(y - _menuItems[1].anchoredPosition.y) ? 0 : 1;
        if (index == _selectIndex) return;
        _selectIndex = index;
        ResetAnimation();
    }

    private void RequestConfirmation()
    {
        if (_confirmationRequested || !_menu.DecideCommand()) return;
        _confirmationRequested = true;
        _menuWasEnabled = _menu.enabled;
        _menu.enabled = false;
        ResetAnimation();
        if (_previewsReady) StartCoroutine(PrepareConfirmation());
    }

    private IEnumerator PrepareConfirmation()
    {
        _state = TitleMenuAnimationState.PreparingConfirmation;
        yield return LoadFrames(_selectIndex, PreviewFrameCount, ConfirmFrameCount);
        if (!FramesReady(_selectIndex, ConfirmFrameCount))
        {
            Debug.LogError("[TitleMenuAnimation] Confirmation sequence is incomplete.");
            RestoreSelection();
            yield break;
        }
        ResetAnimation();
        _state = TitleMenuAnimationState.Confirming;
    }

    private void RestoreSelection()
    {
        if (_confirmationRequested) _menu.enabled = _menuWasEnabled;
        _confirmationRequested = false;
        _state = _previewsReady ? TitleMenuAnimationState.Selecting : TitleMenuAnimationState.Loading;
        ResetAnimation();
    }

    private IEnumerator LoadFrames(int index, int first, int count)
    {
        Texture2D[] frames = _frames[index];
        string path = index == 0 ? GrandPrixPath : EndlessPath;
        for (int frame = first; frame < count; frame++)
        {
            if (frames[frame] != null) continue;
            // 提供された両方の連番画像で、この範囲は0フレーム目と同じ内容になっている。
            if (frame <= 5 || (frame >= 20 && frame <= 40))
            {
                frames[frame] = frames[0];
                continue;
            }
            ResourceRequest request = Resources.LoadAsync<Texture2D>(path + frame.ToString("D5"));
            yield return request;
            frames[frame] = request.asset as Texture2D;
            if (frames[frame] == null)
            {
                Debug.LogError($"[TitleMenuAnimation] Missing texture: {path}{frame:D5}");
                yield break;
            }
        }
    }

    private bool FramesReady(int index, int count)
    {
        for (int frame = 0; frame < count; frame++)
            if (_frames[index][frame] == null) return false;
        return true;
    }

    private void ResetAnimation()
    {
        _elapsedTime = 0;
        _currentFrame = -1;
        DrawFrame(0);
    }

    private void DrawFrame(int frame)
    {
        if (frame == _currentFrame) return;
        _currentFrame = frame;
        _image.texture = _frames[_selectIndex][frame];
    }

    private bool TryClickedMode(out int index)
    {
        index = 0;
        Mouse mouse = Mouse.current;
        if (mouse == null || !mouse.leftButton.wasPressedThisFrame) return false;
        RectTransform imageRect = _image.rectTransform;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(imageRect,
            mouse.position.ReadValue(), null, out Vector2 point)) return false;
        Rect rect = imageRect.rect;
        if (!rect.Contains(point)) return false;
        float x = (point.x - rect.xMin) / rect.width;
        float y = (point.y - rect.yMin) / rect.height;
        if (x < 0.54f || x > 0.96f || y < 0.01f || y > 0.62f) return false;
        index = y >= 0.3116f ? 0 : 1;
        return true;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (_confirmationRequested && _menu != null) _menu.enabled = _menuWasEnabled;
        if (_image != null) _image.texture = null;
        var released = new HashSet<Texture2D>();
        foreach (Texture2D[] frames in _frames)
        {
            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i] != null && released.Add(frames[i])) Resources.UnloadAsset(frames[i]);
                frames[i] = null;
            }
        }
    }
}
