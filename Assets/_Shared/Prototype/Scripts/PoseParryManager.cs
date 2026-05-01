using UnityEngine;

// パリイ判定マネージャークラス
public class PoseParryManager : MonoBehaviour
{
    private PoseId currentEnemyPoseId;


    void Start()
    {
        currentEnemyPoseId = PoseId.None;
    }

    public void SetEnemyPose(PoseId poseId)
    {
        currentEnemyPoseId = poseId;
    }

    public bool CheckParry(PoseId playerPoseId)
    {
        return playerPoseId == currentEnemyPoseId;
    }

}
