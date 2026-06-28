using UnityEngine;
using System;

namespace Pose
{
    // パリイの判定を行うクラス
    public class PoseParryJudge : MonoBehaviour
    {
        // 敵のPoseID
        [SerializeField] private PoseID _enemyPoseID;

        // プレイヤーのPoseID
        [SerializeField] private PoseID _playerPoseID;

        // ポーズ中かどうかを管理するクラス
        [SerializeField] private PoseStateTracker _stateTracker;

        // パリイ発生時のアクション
        public event Action OnParrySuccess;

        private void Start()
        {
            _enemyPoseID = PoseID.None;
            _playerPoseID = PoseID.None;

            if (_stateTracker == null)
            {
                _stateTracker = FindFirstObjectByType<PoseStateTracker>();
            }
        }

        public void StartEnemyPose(PoseID enemyPoseID)
        {
            if (_enemyPoseID == PoseID.None && enemyPoseID != PoseID.None)
            {
                _stateTracker?.BeginPose();
            }

            _enemyPoseID = enemyPoseID;
        }
        public void StartPlayerPose(PoseID playerPoseID)
        {
            if (_playerPoseID == PoseID.None && playerPoseID != PoseID.None)
            {
                _stateTracker?.BeginPose();
            }

            _playerPoseID = playerPoseID;
            CheckParry();
        }
        public void EndEnemyPose()
        {
            if (_enemyPoseID != PoseID.None)
            {
                _stateTracker?.EndPose();
            }

            _enemyPoseID = PoseID.None;
            Debug.Log("PoseParryJudge Enemy pose ended.");
        }
        public void EndPlayerPose()
        {
            if (_playerPoseID != PoseID.None)
            {
                _stateTracker?.EndPose();
            }

            _playerPoseID = PoseID.None;
            Debug.Log("PoseParryJudge Player pose ended.");
        }

        private void CheckParry()
        {
            Debug.Log($"PoseParryJudge CheckParry() Player pose: {_playerPoseID}");
            Debug.Log($"PoseParryJudge CheckParry() Enemy pose: {_enemyPoseID}");
            if (_enemyPoseID == PoseID.None) { return; }
            if (_playerPoseID == PoseID.None) { return; }

            if (IsParrySuccessful())
            {
                Debug.Log("Parry Successful!");
                // パリイ成功時の処理
                OnParrySuccess?.Invoke();
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
            return _enemyPoseID != PoseID.None && _playerPoseID != PoseID.None && _enemyPoseID == _playerPoseID;
        }

        // デバッグ用のメソッド
        public void DebugOnParry()
        {
            Debug.Log("Debug: Parry triggered.");
            OnParrySuccess?.Invoke();
        }
    }
}
