using Enemy;
using Player;
using UnityEngine;
//using Enemy;
//using Pose;

namespace AudienceRequest
{
    public class AudienceRequestJudge : MonoBehaviour
    {
        //
        [SerializeField] private AudienceRequestContext _context;
        [SerializeField] private AudienceRequestSystem _requestSystem;

        // Player
        private PlayerPoseContoroller _playerPoseController;
        // Enemy
        private EnemyContext _enemyContext;

        private void Start()
        {
            // PlayerPoseControllerの取得
            _playerPoseController = _context.GetPlayerContext()?.GetPlayerPoseContoroller;
            Debug.Log("AudienceRequestJudge Start. playerPoseController = " + _playerPoseController);

            // PlayerPoseControllerのイベントにリクエスト判定関数を登録
            if (_playerPoseController == null) { return; }
            _playerPoseController.OnPoseStart += JudgeRequest;

            // EnemyContextの取得
            _enemyContext = _context.GetEnemyContext();
            // EnemyContextのイベントにリクエスト判定関数を登録
            if (_enemyContext != null)
            {
                _enemyContext.OnEnemyPoseStart += JudgeRequest;
                Debug.Log("AudienceRequestJudge: Subscribed to EnemyContext OnEnemyPoseStart.");
            }
        }

        // メモリリーク防止のため、オブジェクト破棄時にイベント登録を解除 
        private void OnDestroy()
        {
            if (_playerPoseController != null)
            {
                _playerPoseController.OnPoseStart -= JudgeRequest;
            }
            if (_enemyContext != null)
            {
                _enemyContext.OnEnemyPoseStart -= JudgeRequest;
            }
        }


        // リクエスト判定関数
        // ポーズ時に対応したリクエストをクリアし、個数分のポイントを加算する
        public void JudgeRequest(Pose.PoseID poseID)
        {
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
