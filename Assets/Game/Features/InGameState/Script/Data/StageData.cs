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
    [SerializeField] private Vector3 cameraInitialPosition;

    [Header("Enemy")]
    [SerializeField] private EnemyMaxCount initialEnemyCount = EnemyMaxCount.Two;
    [SerializeField] private EnemyMaxCount maxEnemyCount = EnemyMaxCount.Two;
    [SerializeField] private EnemyLineupEntry[] initialEnemies;

    [Header("Wave")]
    [SerializeField] private bool endlessMode;
    [SerializeField] private StageWaveEntry[] waves;

    [Header("Endless Wave")]
    [SerializeField] private StageWaveEntry endlessFirstWave;
    [SerializeField] private WaveDataList endlessWaveDataList;
    [SerializeField, Min(1)] private int endlessTargetScoreIncreasePerWave = 1000;

    public InGamePresentationAction GameStartAction => gameStartAction;
    public InGamePresentationAction GameEndAction => gameEndAction;
    public string ResultSceneName => resultSceneName;
    public Vector3 CameraInitialPosition => cameraInitialPosition;
    public EnemyMaxCount InitialEnemyCount => initialEnemyCount == EnemyMaxCount.Four
        ? EnemyMaxCount.Four
        : EnemyMaxCount.Two;
    public EnemyMaxCount MaxEnemyCount => InitialEnemyCount == EnemyMaxCount.Four || maxEnemyCount == EnemyMaxCount.Four
        ? EnemyMaxCount.Four
        : EnemyMaxCount.Two;
    public EnemyLineupEntry[] InitialEnemies => initialEnemies;
    public bool HasInitialEnemies => initialEnemies != null && initialEnemies.Length > 0;
    public bool IsEndlessMode => endlessMode;
    public int WaveCount => waves != null ? waves.Length : 0;

    private void OnValidate()
    {
        if (initialEnemyCount == EnemyMaxCount.Four)
        {
            maxEnemyCount = EnemyMaxCount.Four;
        }
    }

    public StageWaveEntry GetWaveEntryOrNull(int waveIndex)
    {
        if (endlessMode)
        {
            return waveIndex == 0 ? endlessFirstWave : null;
        }

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

    // Wave番号に対応するWaveData取得
    public WaveData GetWaveOrNull(int waveIndex)
    {
        if (waveIndex < 0)
        {
            return null;
        }

        if (endlessMode && waveIndex > 0)
        {
            return endlessWaveDataList != null
                ? endlessWaveDataList.GetRandomWaveOrNull()
                : null;
        }

        return GetWaveEntryOrNull(waveIndex)?.WaveData;
    }

    public int GetTargetScore(int waveIndex)
    {
        if (waveIndex < 0)
        {
            return 0;
        }

        if (endlessMode)
        {
            int firstTargetScore = endlessFirstWave != null ? endlessFirstWave.TargetScore : 0;
            int scoreIncrease = Mathf.Max(1, endlessTargetScoreIncreasePerWave);
            long targetScore = (long)firstTargetScore + (long)scoreIncrease * waveIndex;
            return targetScore >= int.MaxValue ? int.MaxValue : (int)targetScore;
        }

        StageWaveEntry entry = GetWaveEntryOrNull(waveIndex);
        return entry != null ? entry.TargetScore : 0;
    }

    // Wave番号の存在確認
    public bool HasWave(int waveIndex)
    {
        if (waveIndex < 0)
        {
            return false;
        }

        if (!endlessMode)
        {
            return GetWaveEntryOrNull(waveIndex)?.WaveData != null;
        }

        if (waveIndex == 0)
        {
            return endlessFirstWave?.WaveData != null;
        }

        return endlessFirstWave?.WaveData != null &&
               endlessWaveDataList != null &&
               endlessWaveDataList.HasWaveData;
    }
}
