using Enemy;
using UnityEngine;

[CreateAssetMenu(fileName = "WaveData", menuName = "Game/InGame/Wave Data")]
public sealed class WaveData : ScriptableObject
{
    [SerializeField] private InGamePresentationAction wavePrevAction;
    [InspectorName("Wave Start Action (Below Capacity)")]
    [SerializeField] private InGamePresentationAction waveStartAction;
    // 既存アセットの参照維持のためシリアライズ名を保持
    [InspectorName("Wave Start Action (At Capacity)")]
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

    // 新敵追加前の確定済みEnemy数と実際の上限から開始演出を取得
    public InGamePresentationAction GetWaveStartAction(int currentEnemyCount, int maxEnemyCount)
    {
        if (maxEnemyCount > 0 && currentEnemyCount >= maxEnemyCount && waveStartActionWhenFourEnemies != null)
        {
            return waveStartActionWhenFourEnemies;
        }

        return waveStartAction;
    }
}
