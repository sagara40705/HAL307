using UnityEngine;

/// <summary>
/// Playerのポーズ時間と表示色を保持する設定データ。
/// </summary>
[CreateAssetMenu(fileName = "PlayerPoseSettings", menuName = "Game/InGame/Player Pose Settings")]
public sealed class PlayerPoseSettings : ScriptableObject
{
    [SerializeField, Min(0f)] private float poseDuration = 7f;

    [Header("Pose Color")]
    [SerializeField] private Color yaaColor = new Color(0f, 0.41078663f, 1f, 0f);
    [SerializeField] private Color powerColor = new Color(1f, 0f, 0f, 0f);
    [SerializeField] private Color muscleColor = new Color(0.9113277f, 1f, 0f, 0f);

    public float PoseDuration => Mathf.Max(0f, poseDuration);
    public Color YaaColor => yaaColor;
    public Color PowerColor => powerColor;
    public Color MuscleColor => muscleColor;
}
