using UnityEngine;

/// <summary>
/// スコア加算ルールを保持する設定データ。
/// </summary>
[CreateAssetMenu(fileName = "ScoreSettings", menuName = "Game/InGame/Score Settings")]
public sealed class ScoreSettings : ScriptableObject
{
    [SerializeField, Min(0)] private int scoreUnit = 100;
    [SerializeField, Min(1)] private int feverScoreMultiplier = 2;

    public int ScoreUnit => Mathf.Max(0, scoreUnit);
    public int FeverScoreMultiplier => Mathf.Max(1, feverScoreMultiplier);
}
