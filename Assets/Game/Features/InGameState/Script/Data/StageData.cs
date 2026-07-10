using Enemy;
using UnityEngine;

[CreateAssetMenu(fileName = "StageData", menuName = "Game/InGame/Stage Data")]
public sealed class StageData : ScriptableObject
{
    [Header("Presentation")]
    [SerializeField] private InGamePresentationAction gameStartAction;
    [SerializeField] private InGamePresentationAction gameEndAction;

    [Header("Scene")]
    [SerializeField] private string resultSceneName = "Result";

    [Header("Enemy")]
    [SerializeField] private EnemyLineupEntry[] initialEnemies;

    [Header("Wave")]
    [SerializeField] private WaveData[] waves;

    public InGamePresentationAction GameStartAction => gameStartAction;
    public InGamePresentationAction GameEndAction => gameEndAction;
    public string ResultSceneName => resultSceneName;
    public EnemyLineupEntry[] InitialEnemies => initialEnemies;
    public bool HasInitialEnemies => initialEnemies != null && initialEnemies.Length > 0;
    public int WaveCount => waves != null ? waves.Length : 0;

    // Wave番号に対応するWaveData取得
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

    // Wave番号の存在確認
    public bool HasWave(int waveIndex)
    {
        return GetWaveOrNull(waveIndex) != null;
    }
}
