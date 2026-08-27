using System;
using UnityEngine;

[Serializable]
public sealed class StageWaveEntry
{
    [SerializeField] private WaveData waveData;
    [SerializeField, Min(0)] private int targetScore;
    [SerializeField, Min(0f)] private float enemyAnimationSpeed = 1f;

    public WaveData WaveData => waveData;
    public int TargetScore => targetScore;
    public float EnemyAnimationSpeed => Mathf.Max(0f, enemyAnimationSpeed);
}
