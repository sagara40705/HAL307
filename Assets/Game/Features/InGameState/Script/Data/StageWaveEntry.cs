using System;
using UnityEngine;

[Serializable]
public sealed class StageWaveEntry
{
    [SerializeField] private WaveData waveData;
    [SerializeField, Min(0)] private int targetScore;

    public WaveData WaveData => waveData;
    public int TargetScore => targetScore;
}
