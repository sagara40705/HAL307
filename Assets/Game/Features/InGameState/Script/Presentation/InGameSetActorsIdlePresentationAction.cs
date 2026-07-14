using System.Collections;
using System.Collections.Generic;
using Enemy;
using UnityEngine;

[CreateAssetMenu(fileName = "InGameSetActorsIdlePresentationAction", menuName = "Game/InGame/Presentation/Set Actors Idle")]
public sealed class InGameSetActorsIdlePresentationAction : InGamePresentationAction
{
    public override IEnumerator Play(InGameFlowContext context)
    {
        HashSet<OnStartPose> targets = new();

        AddEnemies(context, targets);
        AddPlayer(targets);

        if (targets.Count <= 0)
        {
            Debug.LogWarning("[InGameSetActorsIdlePresentationAction] OnStartPose was not found under Player or Enemy roots.");
            yield break;
        }

        foreach (OnStartPose target in targets)
        {
            if (target != null)
            {
                target.SetTriggerIdle();
            }
        }
    }

    private static void AddEnemies(InGameFlowContext context, HashSet<OnStartPose> targets)
    {
        EnemyLineupManager lineup = context != null ? context.EnemyLineup : null;
        if (lineup != null)
        {
            AddTarget(lineup.Enemy1, targets);
            AddTarget(lineup.Enemy2, targets);
            AddTarget(lineup.PendingOldEnemy, targets);
            AddTarget(lineup.PendingNewEnemy, targets);
        }

        EnemyRegistry registry = Object.FindFirstObjectByType<EnemyRegistry>();
        if (registry == null)
        {
            return;
        }

        foreach (global::Enemy.Enemy enemy in registry.Enemies)
        {
            AddTarget(enemy, targets);
        }
    }

    private static void AddPlayer(HashSet<OnStartPose> targets)
    {
        Player.PlayerPoseContoroller player =
            Object.FindFirstObjectByType<Player.PlayerPoseContoroller>(FindObjectsInactive.Include);

        if (player == null)
        {
            Debug.LogWarning("[InGameSetActorsIdlePresentationAction] Player was not found.");
            return;
        }

        AddTarget(player, targets);
        AddTarget(player.MoveTargetTransform, targets);
    }

    private static void AddTarget(Component root, HashSet<OnStartPose> targets)
    {
        if (root == null)
        {
            return;
        }

        OnStartPose[] components = root.GetComponentsInChildren<OnStartPose>(true);
        foreach (OnStartPose component in components)
        {
            if (component != null)
            {
                targets.Add(component);
            }
        }
    }
}
