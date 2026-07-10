using System;
using Enemy;
using UnityEngine;

[Serializable]
public sealed class WaveData
{
    [SerializeField, Min(0)] private int targetScore;
    [SerializeField] private InGamePresentationAction waveStartAction;
    [SerializeField] private InGamePresentationAction waveTransitionAction;
    [SerializeField] private EnemyData replacementEnemyData;
    [SerializeField] private Vector3 replacementEnemyPosition;
    [SerializeField] private Vector3 replacementEnemyRotationEuler;

    public int TargetScore => targetScore;
    public InGamePresentationAction WaveStartAction => waveStartAction;
    public InGamePresentationAction WaveTransitionAction => waveTransitionAction;
    public EnemyData ReplacementEnemyData => replacementEnemyData;
    public Vector3 ReplacementEnemyPosition => replacementEnemyPosition;
    public Quaternion ReplacementEnemyRotation => Quaternion.Euler(replacementEnemyRotationEuler);
    public bool HasReplacementEnemy => replacementEnemyData != null;
}
