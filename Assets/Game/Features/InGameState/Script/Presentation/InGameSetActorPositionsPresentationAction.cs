using System.Collections;
using Enemy;
using UnityEngine;

[CreateAssetMenu(fileName = "InGameSetActorPositionsPresentationAction", menuName = "Game/InGame/Presentation/Set Actor Positions")]
public sealed class InGameSetActorPositionsPresentationAction : InGamePresentationAction
{
    [Header("3P Positions (World Space)")]
    [InspectorName("Enemy 1 Position")]
    [SerializeField] private Vector3 threePlayerEnemy1Position;
    [InspectorName("Enemy 2 Position")]
    [SerializeField] private Vector3 threePlayerEnemy2Position;
    [InspectorName("Player Position")]
    [SerializeField] private Vector3 threePlayerPlayerPosition;

    // 既存のシリアライズ名を5P用として保持
    [Header("5P Positions (World Space)")]
    [SerializeField] private Vector3 enemy1Position;
    [SerializeField] private Vector3 enemy2Position;
    [SerializeField] private Vector3 enemy3Position;
    [SerializeField] private Vector3 enemy4Position;
    [SerializeField] private Vector3 playerPosition;

    public override IEnumerator Play(InGameFlowContext context)
    {
        EnemyLineupManager lineup = context != null ? context.EnemyLineup : null;
        Player.PlayerPoseContoroller player = Object.FindFirstObjectByType<Player.PlayerPoseContoroller>();

        // 現在人数ではなく選択済みモードで配置を決定。5Pで敵が少ないWaveも5P用を使用
        GameModeManager gameModeManager = GameModeManager.Instance;
        bool useFivePlayerPositions = gameModeManager != null && gameModeManager.IsInitialized &&
            gameModeManager.CurrentGameMode == GameMode.FivePlayer;
        if (gameModeManager == null || !gameModeManager.IsInitialized)
        {
            Debug.LogWarning("[InGameSetActorPositionsPresentationAction] GameModeManager is not ready. ThreePlayer positions are used.");
        }

        SetEnemyPosition(lineup, EnemySlotId.Enemy1, useFivePlayerPositions ? enemy1Position : threePlayerEnemy1Position, true);
        SetEnemyPosition(lineup, EnemySlotId.Enemy2, useFivePlayerPositions ? enemy2Position : threePlayerEnemy2Position, !useFivePlayerPositions);
        if (useFivePlayerPositions)
        {
            SetEnemyPosition(lineup, EnemySlotId.Enemy3, enemy3Position, false);
            SetEnemyPosition(lineup, EnemySlotId.Enemy4, enemy4Position, false);
        }
        SetPlayerPosition(player, useFivePlayerPositions ? playerPosition : threePlayerPlayerPosition);

        yield break;
    }

    public Vector3 GetFivePlayerEnemyPosition(EnemySlotId slot)
    {
        switch (slot)
        {
            case EnemySlotId.Enemy1: return enemy1Position;
            case EnemySlotId.Enemy2: return enemy2Position;
            case EnemySlotId.Enemy3: return enemy3Position;
            case EnemySlotId.Enemy4: return enemy4Position;
            default: throw new System.ArgumentOutOfRangeException(nameof(slot));
        }
    }

    private void SetEnemyPosition(EnemyLineupManager lineup, EnemySlotId slot, Vector3 position, bool isRequired)
    {
        if (lineup == null)
        {
            Debug.LogWarning("[InGameSetActorPositionsPresentationAction] EnemyLineupManager was not found.");
            return;
        }

        global::Enemy.Enemy enemy = lineup.GetEnemy(slot);
        if (enemy == null)
        {
            if (isRequired)
            {
                Debug.LogWarning($"[InGameSetActorPositionsPresentationAction] Enemy was not found. Slot={slot}");
            }

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
