using UnityEngine;

// WaveDataの候補一覧とランダム選択を管理する
[CreateAssetMenu(fileName = "WaveDataList", menuName = "Game/InGame/Wave Data List")]
public sealed class WaveDataList : ScriptableObject
{
    [SerializeField] private WaveData[] waves;

    public bool HasWaveData
    {
        get
        {
            if (waves == null)
            {
                return false;
            }

            foreach (WaveData wave in waves)
            {
                if (wave != null)
                {
                    return true;
                }
            }

            return false;
        }
    }

    // nullを除いた候補からWaveDataをランダムに取得する
    public WaveData GetRandomWaveOrNull()
    {
        if (waves == null || waves.Length <= 0)
        {
            return null;
        }

        int validWaveCount = 0;
        foreach (WaveData wave in waves)
        {
            if (wave != null)
            {
                validWaveCount++;
            }
        }

        if (validWaveCount <= 0)
        {
            return null;
        }

        int randomIndex = Random.Range(0, validWaveCount);
        foreach (WaveData wave in waves)
        {
            if (wave == null)
            {
                continue;
            }

            if (randomIndex == 0)
            {
                return wave;
            }

            randomIndex--;
        }

        return null;
    }
}
