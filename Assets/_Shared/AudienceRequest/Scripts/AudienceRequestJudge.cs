using UnityEngine;
using Player;

namespace AudienceRequest
{
    public class AudienceRequestJudge : MonoBehaviour
    {
        //
        [SerializeField] private AudienceRequestContext _context;
        [SerializeField] private AudienceRequestSystem _requestSystem;

        // Player
        private PlayerPoseContoroller _playerPoseController;

        private void Start()
        {
            // PlayerPoseControllerの取得
            _playerPoseController = _context.GetPlayerContext()?.GetPlayerPoseContoroller;
            Debug.Log("AudienceRequestJudge Start. playerPoseController = " + _playerPoseController);

            // PlayerPoseControllerのイベントにリクエスト判定関数を登録
            if (_playerPoseController == null) { return; }
            _playerPoseController.OnPoseStart += JudgeRequest;
        }

        // リクエスト判定関数
        // ポーズ時に対応したリクエストをクリアし、個数分のポイントを加算する
        public void JudgeRequest(Pose.PoseID poseID)
        {
            if (_playerPoseController == null)
            {
                Debug.LogWarning("PlayerPoseContoroller is not assigned in AudienceRequestJudge.");
                return;
            }

            // 対応したリクエストの個数を取得
            int requestCount = _requestSystem.GetRequestCount(poseID);
            if (requestCount > 0)
            {
                _requestSystem.ClearRequest(poseID);
                Debug.Log("Cleared " + requestCount + " requests for pose: " + poseID);
                // ポイント加算の処理をここに追加
            }
            else
            {
                Debug.Log("No requests to clear for pose: " + poseID);
            }
        }

    }
}
