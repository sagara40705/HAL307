using System.Collections;
using Enemy;
using UnityEngine;

[CreateAssetMenu(fileName = "InGameEnemyIntroMovePresentationAction", menuName = "Game/InGame/Presentation/Enemy Intro Move")]
public sealed class InGameEnemyIntroMovePresentationAction : InGamePresentationAction
{
    [SerializeField, Min(0f)] private float totalDurationSeconds = 3f;
    [SerializeField, Min(0f)] private float enemyMoveSeconds = 1f;
    [SerializeField] private Vector3 moveDirection = Vector3.forward;
    [SerializeField, Min(0f)] private float moveSpeed = 1f;

    public override IEnumerator Play(InGameFlowContext context)
    {
        EnemyLineupManager lineup = context != null ? context.EnemyLineup : null;
        if (lineup == null)
        {
            Debug.LogWarning("[InGameEnemyIntroMovePresentationAction] EnemyLineupManager was not found.");
            yield break;
        }

        Player.PlayerPoseContoroller player = Object.FindFirstObjectByType<Player.PlayerPoseContoroller>();
        yield return MoveTargets(lineup.Enemy1, lineup.Enemy2, player);
    }

    private IEnumerator MoveTargets(global::Enemy.Enemy enemy1, global::Enemy.Enemy enemy2, Player.PlayerPoseContoroller player)
    {
        Transform enemy1Transform = enemy1 != null ? enemy1.transform : null;
        Transform enemy2Transform = enemy2 != null ? enemy2.transform : null;
        Transform playerTransform = player != null ? player.MoveTargetTransform : null;

        if (enemy1Transform == null && enemy2Transform == null && playerTransform == null)
        {
            yield break;
        }

        Vector3 enemy1StartPosition = enemy1Transform != null ? enemy1Transform.position : Vector3.zero;
        Vector3 enemy2StartPosition = enemy2Transform != null ? enemy2Transform.position : Vector3.zero;
        Vector3 playerStartPosition = playerTransform != null ? playerTransform.position : Vector3.zero;
        Vector3 direction = GetMoveDirection();

        if (totalDurationSeconds <= 0f)
        {
            ApplyMovePosition(enemy1Transform, enemy1StartPosition, direction, 0f);
            ApplyMovePosition(enemy2Transform, enemy2StartPosition, direction, 0f);
            ApplyMovePosition(playerTransform, playerStartPosition, direction, 0f);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < totalDurationSeconds)
        {
            ApplyMovePosition(enemy1Transform, enemy1StartPosition, direction, elapsed);
            ApplyMovePosition(enemy2Transform, enemy2StartPosition, direction, elapsed);
            ApplyMovePosition(playerTransform, playerStartPosition, direction, elapsed);

            yield return null;
            elapsed += Time.deltaTime;
        }

        ApplyMovePosition(enemy1Transform, enemy1StartPosition, direction, totalDurationSeconds);
        ApplyMovePosition(enemy2Transform, enemy2StartPosition, direction, totalDurationSeconds);
        ApplyMovePosition(playerTransform, playerStartPosition, direction, totalDurationSeconds);
    }

    private Vector3 GetMoveDirection()
    {
        if (moveDirection.sqrMagnitude <= 0f)
        {
            return Vector3.forward;
        }

        return moveDirection.normalized;
    }

    private void ApplyMovePosition(Transform target, Vector3 startPosition, Vector3 direction, float elapsed)
    {
        if (target == null)
        {
            return;
        }

        float moveElapsed = Mathf.Clamp(elapsed, 0f, enemyMoveSeconds);
        target.position = startPosition + direction * moveSpeed * moveElapsed;
    }
}
