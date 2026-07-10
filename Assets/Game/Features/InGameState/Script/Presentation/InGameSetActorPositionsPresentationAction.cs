using System.Collections;
using Enemy;
using UnityEngine;

[CreateAssetMenu(fileName = "InGameSetActorPositionsPresentationAction", menuName = "Game/InGame/Presentation/Set Actor Positions")]
public sealed class InGameSetActorPositionsPresentationAction : InGamePresentationAction
{
    [SerializeField] private Vector3 enemy1Position;
    [SerializeField] private Vector3 enemy2Position;
    [SerializeField] private Vector3 playerPosition;

    public override IEnumerator Play(InGameFlowContext context)
    {
        EnemyLineupManager lineup = context != null ? context.EnemyLineup : null;
        Player.PlayerPoseContoroller player = Object.FindFirstObjectByType<Player.PlayerPoseContoroller>();

        SetEnemyPosition(lineup, EnemySlotId.Enemy1, enemy1Position);
        SetEnemyPosition(lineup, EnemySlotId.Enemy2, enemy2Position);
        SetPlayerPosition(player, playerPosition);

        yield break;
    }

    private void SetEnemyPosition(EnemyLineupManager lineup, EnemySlotId slot, Vector3 position)
    {
        if (lineup == null)
        {
            Debug.LogWarning("[InGameSetActorPositionsPresentationAction] EnemyLineupManager was not found.");
            return;
        }

        global::Enemy.Enemy enemy = lineup.GetEnemy(slot);
        if (enemy == null)
        {
            Debug.LogWarning($"[InGameSetActorPositionsPresentationAction] Enemy was not found. Slot={slot}");
            return;
        }

        enemy.transform.position = position;
    }

    private void SetPlayerPosition(Player.PlayerPoseContoroller player, Vector3 position)
    {
        if (player == null)
        {
            Debug.LogWarning("[InGameSetActorPositionsPresentationAction] Player was not found.");
            return;
        }

        Transform playerTransform = player.MoveTargetTransform;
        if (playerTransform == null)
        {
            Debug.LogWarning("[InGameSetActorPositionsPresentationAction] Player move target was not found.");
            return;
        }

        playerTransform.position = position;
    }
}
