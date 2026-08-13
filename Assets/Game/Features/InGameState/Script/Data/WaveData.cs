using Enemy;
using UnityEngine;

[CreateAssetMenu(fileName = "WaveData", menuName = "Game/InGame/Wave Data")]
public sealed class WaveData : ScriptableObject
{
    [SerializeField] private InGamePresentationAction wavePrevAction;
    [SerializeField] private InGamePresentationAction waveStartAction;
    [SerializeField] private InGamePresentationAction waveTransitionAction;
    [SerializeField] private EnemyData replacementEnemyData;
    [SerializeField] private Vector3 replacementEnemyPosition;
    [SerializeField] private Vector3 replacementEnemyRotationEuler;

    public InGamePresentationAction WavePrevAction => wavePrevAction;
    public InGamePresentationAction WaveStartAction => waveStartAction;
    public InGamePresentationAction WaveTransitionAction => waveTransitionAction;
    public EnemyData ReplacementEnemyData => replacementEnemyData;
    public Vector3 ReplacementEnemyPosition => replacementEnemyPosition;
    public Quaternion ReplacementEnemyRotation => Quaternion.Euler(replacementEnemyRotationEuler);
    public bool HasReplacementEnemy => replacementEnemyData != null;
}
