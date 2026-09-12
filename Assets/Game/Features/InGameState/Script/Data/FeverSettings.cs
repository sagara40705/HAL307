using UnityEngine;

/// <summary>
/// フィーバーのゲージ加算と継続ルールを保持する設定データ。
/// </summary>
[CreateAssetMenu(fileName = "FeverSettings", menuName = "Game/InGame/Fever Settings")]
public sealed class FeverSettings : ScriptableObject
{
    [SerializeField, Range(0f, 1f)] private float fillIncrease = 0.05f;
    [SerializeField, Min(0f)] private float feverDuration = 10f;
    [SerializeField] private bool resetFillOnFeverEnd = true;
    [SerializeField, Min(0f)] private float cutInDisplayTime = 3f;

    public float FillIncrease => Mathf.Clamp01(fillIncrease);
    public float FeverDuration => Mathf.Max(0f, feverDuration);
    public bool ResetFillOnFeverEnd => resetFillOnFeverEnd;
    public float CutInDisplayTime => Mathf.Max(0f, cutInDisplayTime);
}
