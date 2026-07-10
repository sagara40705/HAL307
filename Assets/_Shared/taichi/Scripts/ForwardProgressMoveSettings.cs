using UnityEngine;

[CreateAssetMenu(fileName = "ForwardProgressMoveSettings", menuName = "Game/Stage/Forward Progress Move Settings")]
public sealed class ForwardProgressMoveSettings : ScriptableObject
{
    [SerializeField, Min(0f)] private float enemyMoveSpeed = 1f;
    [SerializeField, Min(0f)] private float playerMoveSpeed = 1f;
    [SerializeField, Range(0f, 1f)] private float leaderSpeedMultiplier = 0.5f;

    public float EnemyMoveSpeed => enemyMoveSpeed;
    public float PlayerMoveSpeed => playerMoveSpeed;
    public float LeaderSpeedMultiplier => leaderSpeedMultiplier;
}
