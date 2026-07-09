using System.Collections.Generic;
using UnityEngine;

public class MenuSelectCursor
{
    [SerializeField] private RectTransform _CursorRtf; // ダンベカーソルのtransform
    [SerializeField] private RectTransform _SelectFlameRtf; // 選択枠のtransform

    private const int _SelectPositionCount = (int)MenuCommand.Count; // 選択肢の数

    [SerializeField] private Vector2[] _CursorPosition = new Vector2[_SelectPositionCount]; // カーソルの位置を格納する配列
    [SerializeField] private Vector2[] _SelectFlamePosition = new Vector2[_SelectPositionCount]; // 選択枠の位置を格納する配列

    public void SetCursorPosition(int index)
    {
        if (index < 0 || index >= _SelectPositionCount)
        {
            Debug.LogError("Invalid index for cursor position.");
            return;
        }
        _CursorRtf.anchoredPosition = _CursorPosition[index];
        _SelectFlameRtf.anchoredPosition = _SelectFlamePosition[index];
    }
}
