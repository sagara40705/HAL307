using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using static Sound.Manager.SoundManager;

//　オプション
public enum TitleMenuItem
{
    Muscle_GrandPrix,
    Muscle_Trial,
    Count,
}

public class TitleUiManager : MonoBehaviour
{
    private TitleMenuItem mTitleMenuItem;

    // カーソルのUI（RectTransform）
    [SerializeField]
    private RectTransform mCursor;

    // カーソルの初期位置（アンカー座標）
    [SerializeField]
    private Vector2 mCursorInitialAnchoredPos;

    // メニューアイテムのUI（上から順に配置）。TitleMenuItem の列挙順に対応すること
    [SerializeField]
    private RectTransform[] mMenuItemRects = new RectTransform[(int)TitleMenuItem.Count];

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        mTitleMenuItem = TitleMenuItem.Muscle_GrandPrix;    //初期化
        // カーソル初期位置の保存（インスペクタで指定されていなければ現在の位置を使う）
        if (mCursor != null)
        {
            if (mCursorInitialAnchoredPos == default)
            {
                mCursorInitialAnchoredPos = mCursor.anchoredPosition;
            }
            // 最初の選択に合わせてカーソルを配置
            SetCursurPos();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (DebugShortcuts.Pressed(UnityEngine.InputSystem.Key.UpArrow))
        {
            UpCommand();
            return;
        }
        if (DebugShortcuts.Pressed(UnityEngine.InputSystem.Key.DownArrow))
        {
            DownCommand();
            return;
        }
        if (DebugShortcuts.Pressed(UnityEngine.InputSystem.Key.Space))
        {
            DecideCommand();
            return;
        }
    }

    // メニューの上移動コマンド
    public bool UpCommand()
    {
        int index = (int)mTitleMenuItem;
        if (index > 0)
        {
            index--;
            mTitleMenuItem = (TitleMenuItem)index;
            return SetCursurPos();
        }

        return false;
    }

    // メニューの下移動コマンド
    public bool DownCommand()
    {
        int index = (int)mTitleMenuItem;
        if (index < (int)TitleMenuItem.Count - 1)
        {
            index++;
            mTitleMenuItem = (TitleMenuItem)index;
            return SetCursurPos();
        }

        return false;
    }

    // メニューの決定コマンド（選択確定）
    // mTitleMenuItem に応じて GameModeManager.TrySetGameMode を呼ぶ
    public bool DecideCommand()
    {
        // GameModeManager の存在確認
        var gm = GameModeManager.Instance;
        if (gm == null)
        {
            Debug.LogWarning("GameModeManager instance is not available.");
            return false;
        }

        switch (mTitleMenuItem)
        {
            case TitleMenuItem.Muscle_GrandPrix:
                return gm.TrySetGameMode(GameMode.ThreePlayer);

            case TitleMenuItem.Muscle_Trial:
                return gm.TrySetGameMode(GameMode.FivePlayer);

            default:
                Debug.LogWarning($"DecideCommand: No mapping for menu item {mTitleMenuItem}");
                return false;
        }
    }

    // メニューの決定コマンド
    private bool SetCursurPos()
    {
        // メニューアイテムのインデックスを取得
        int index = (int)mTitleMenuItem;
        if (index < 0 || index >= (int)TitleMenuItem.Count)
        {
            Debug.LogError("Invalid menu item index");
            return false;
        }

        if (mCursor == null)
        {
            Debug.LogWarning("Cursor RectTransform is not assigned.");
            return true;
        }

        if (mMenuItemRects == null || mMenuItemRects.Length <= index || mMenuItemRects[index] == null)
        {
            // メニューアイテムが未設定の場合は初期位置に戻す
            mCursor.anchoredPosition = mCursorInitialAnchoredPos;
            Debug.Log($"Cursor set to initial position: {mCursorInitialAnchoredPos}");
            return true;
        }

        // メニューアイテムの Y 座標にカーソルを合わせる（アンカー座標で合わせる）
        var targetAnchored = mMenuItemRects[index].anchoredPosition;
        mCursor.anchoredPosition = new Vector2(mCursor.anchoredPosition.x, targetAnchored.y);
        Debug.Log($"Cursor set to menu index {index} at y={targetAnchored.y}");
        return true;
    }

    
}
