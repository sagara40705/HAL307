using Enemy;
using UnityEngine;

[CreateAssetMenu(fileName = "WaveData", menuName = "Game/InGame/Wave Data")]
public sealed class WaveData : ScriptableObject
{
    [SerializeField] private InGamePresentationAction wavePrevAction;
    [InspectorName("Wave Start Action (Under 4 Enemies)")]
    [SerializeField] private InGamePresentationAction waveStartAction;
    [InspectorName("Wave Start Action (4 Enemies)")]
    [SerializeField] private InGamePresentationAction waveStartActionWhenFourEnemies;
    [SerializeField] private InGamePresentationAction waveTransitionAction;

    [Header("Enemy")]
    [SerializeField] private EnemyData replacementEnemyData;
    [SerializeField] private Vector3 replacementEnemyPosition;
    [SerializeField] private Vector3 replacementEnemyRotationEuler;

    public InGamePresentationAction WavePrevAction => wavePrevAction;
    public InGamePresentationAction WaveStartAction => waveStartAction;
    public InGamePresentationAction WaveStartActionWhenFourEnemies => waveStartActionWhenFourEnemies;
    public InGamePresentationAction WaveTransitionAction => waveTransitionAction;
    public EnemyData ReplacementEnemyData => replacementEnemyData;
    public Vector3 ReplacementEnemyPosition => replacementEnemyPosition;
    public Quaternion ReplacementEnemyRotation => Quaternion.Euler(replacementEnemyRotationEuler);
    public bool HasReplacementEnemy => replacementEnemyData != null;

    // Wave開始前の確定済みEnemy数から開始演出を取得
    public InGamePresentationAction GetWaveStartAction(int currentEnemyCount)
    {
        if (currentEnemyCount >= 4 && waveStartActionWhenFourEnemies != null)
        {
            return waveStartActionWhenFourEnemies;
        }

        return waveStartAction;
    }
}
