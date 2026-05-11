using UnityEngine;
using System;

namespace Pose
{
    // パリイの判定を行うクラス
    public class PoseParryJudge : MonoBehaviour
    {
        // 敵のPoseID
        [SerializeField] private PoseID enemyPoseID;

        // プレイヤーのPoseID
        [SerializeField] private PoseID playerPoseID;

        // パリイ発生時のアクション
        public event Action OnParrySuccess;

        public void StartEnemyPose(PoseID enemyPoseID)
        {
            this.enemyPoseID = enemyPoseID;
        }
        public void StartPlayerPose(PoseID playerPoseID)
        {
            this.playerPoseID = playerPoseID;
            CheckParry();
        }
        public void EndEnemyPose()
        {
            this.enemyPoseID = PoseID.None;
        }
        public void EndPlayerPose()
        {
            this.playerPoseID = PoseID.None;
        }

        private void CheckParry()
        {
            if (enemyPoseID == PoseID.None) { return; }
            if (playerPoseID == PoseID.None) { return; }

            if (IsParrySuccessful())
            {
                Debug.Log("Parry Successful!");
                // パリイ成功時の処理
                OnParrySuccess();
            }
            else
            {
                Debug.Log("Parry Failed!");
                // パリイ失敗時の処理をここに追加
            }
        }

        // パリイの成功判定
        private bool IsParrySuccessful()
        {
            // 敵のPoseIDとプレイヤーのPoseIDが一致しているかを判定
            return enemyPoseID != PoseID.None && playerPoseID != PoseID.None && enemyPoseID == playerPoseID;
        }
    }
}
