using System;
using UnityEngine;

public class MenuController
{
    private int _selectedIndex = 0; // 現在選択されているインデックス

    [SerializeField] private MenuSelectCursor _menuSelectCursor; // メニュー選択カーソルの参照

    // 選択時のアクション
    [SerializeField] private Action[] _menuActions = new Action[(int)MenuCommand.Count];

    // マイク設定UI

    //　操作設定UI

    public void Update()
    {
        if (DebugShortcuts.Pressed(UnityEngine.InputSystem.Key.UpArrow))
        {
            SelectUp();
        }

        if (DebugShortcuts.Pressed(UnityEngine.InputSystem.Key.DownArrow))
        {
            SelectDown();
        }

        if (DebugShortcuts.Pressed(UnityEngine.InputSystem.Key.Enter))
        {
            ExecuteSelectedAction();
        }
    }

    // 上方向の選択
    private void SelectUp()
    {
        _selectedIndex--;   // インデックスを減らす
        if (_selectedIndex < 0)
        {
            _selectedIndex = (int)MenuCommand.Count - 1; // 最後の選択肢に戻る
        }
        _menuSelectCursor?.SetCursorPosition(_selectedIndex);    // カーソルの位置を更新
    }

    // 下方向の選択
    private void SelectDown()
    {
        _selectedIndex++;   // インデックスを増やす
        if (_selectedIndex >= (int)MenuCommand.Count)
        {
            _selectedIndex = 0; // 最初の選択肢に戻る
        }
        _menuSelectCursor?.SetCursorPosition(_selectedIndex);    // カーソルの位置を更新
    }

    // 選択されたアクションを実行
    private void ExecuteSelectedAction()
    {
        if (_selectedIndex >= 0 && _selectedIndex < _menuActions.Length)
        {
            _menuActions[_selectedIndex]?.Invoke(); // 選択されたアクションを実行
        }
    }

    // InGameに戻るアクション
    private void ReturnInGame()
    {
        PauseManager.Instance?.ClosePause();
    }

    // リタイアアクション
    private void Retire()
    {
        // リタイア処理を実装
        Debug.Log("Retire action executed.");
    }

    // マイク設定アクション
    private void MikeSetting()
    {
        // マイク設定処理を実装
        Debug.Log("MikeSetting action executed.");
    }

    // 操作方法アクション
    private void OperationManual()
    {
        // 操作方法処理を実装
        Debug.Log("OperationManual action executed.");
    }

}
