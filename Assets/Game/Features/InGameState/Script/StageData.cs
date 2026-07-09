using UnityEngine;

[CreateAssetMenu(fileName = "StageData", menuName = "Game/InGame/Stage Data")]
public sealed class StageData : ScriptableObject
{
    [Header("Presentation")]
    [SerializeField] private InGamePresentationAction gameStartAction;
    [SerializeField] private InGamePresentationAction gameEndAction;

    [Header("Scene")]
    [SerializeField] private string resultSceneName = "Result";

    [Header("Wave")]
    [SerializeField] private WaveData[] waves;

    public InGamePresentationAction GameStartAction => gameStartAction;
    public InGamePresentationAction GameEndAction => gameEndAction;
    public string ResultSceneName => resultSceneName;
    public int WaveCount => waves != null ? waves.Length : 0;

    // Gets wave data by index.
    public WaveData GetWaveOrNull(int waveIndex)
    {
        if (waves == null)
        {
            return null;
        }

        if (waveIndex < 0 || waveIndex >= waves.Length)
        {
            return null;
        }

        return waves[waveIndex];
    }

    // Checks whether the wave index exists.
    public bool HasWave(int waveIndex)
    {
        return GetWaveOrNull(waveIndex) != null;
    }
}
