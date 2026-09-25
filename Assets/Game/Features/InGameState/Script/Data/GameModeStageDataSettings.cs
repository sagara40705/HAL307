using UnityEngine;

[CreateAssetMenu(
    fileName = "GameModeStageDataSettings",
    menuName = "Game/InGame/Game Mode Stage Data Settings")]
public sealed class GameModeStageDataSettings : ScriptableObject
{
    [SerializeField] private StageData threePlayerStageData;
    [SerializeField] private StageData fivePlayerStageData;

    public StageData ThreePlayerStageData => threePlayerStageData;
    public StageData FivePlayerStageData => fivePlayerStageData;

    // ゲームモードに対応するStageDataを取得
    public StageData GetStageDataOrNull(GameMode gameMode)
    {
        switch (gameMode)
        {
            case GameMode.ThreePlayer:
                return threePlayerStageData;

            case GameMode.FivePlayer:
                return fivePlayerStageData;

            default:
                return null;
        }
    }
}
