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
        Transform[] targets =
        {
            lineup.Enemy1 != null ? lineup.Enemy1.transform : null,
            lineup.Enemy2 != null ? lineup.Enemy2.transform : null,
            lineup.Enemy3 != null ? lineup.Enemy3.transform : null,
            lineup.Enemy4 != null ? lineup.Enemy4.transform : null,
            player != null ? player.MoveTargetTransform : null,
        };

        yield return MoveTargets(targets);
    }

    private IEnumerator MoveTargets(Transform[] targets)
    {
        Vector3[] startPositions = new Vector3[targets.Length];
        bool hasTarget = false;
        for (int i = 0; i < targets.Length; i++)
        {
            Transform target = targets[i];
            if (target == null)
            {
                continue;
            }

            startPositions[i] = target.position;
            hasTarget = true;
        }

        if (!hasTarget)
        {
            yield break;
        }

        Vector3 direction = GetMoveDirection();

        if (totalDurationSeconds <= 0f)
        {
            ApplyMovePositions(targets, startPositions, direction, 0f);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < totalDurationSeconds)
        {
            ApplyMovePositions(targets, startPositions, direction, elapsed);

            yield return null;
            elapsed += Time.deltaTime;
        }

        ApplyMovePositions(targets, startPositions, direction, totalDurationSeconds);
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

    private void ApplyMovePositions(Transform[] targets, Vector3[] startPositions, Vector3 direction, float elapsed)
    {
        for (int i = 0; i < targets.Length; i++)
        {
            ApplyMovePosition(targets[i], startPositions[i], direction, elapsed);
        }
    }
}
