using Enemy;
using Player;
using UnityEngine;

namespace AudienceRequest
{
    public class AudienceRequestJudge : MonoBehaviour
    {
        [SerializeField] private AudienceRequestContext _context;
        [SerializeField] private AudienceRequestSystem _requestSystem;

        [Header("音声クリア時のテキスト演出")]
        [SerializeField]
        private AudienceRequestTextAnimationController _textAnimationController;

        private PlayerPoseContoroller _playerPoseController;
        private EnemyContext _enemyContext;

        private void Start()
        {
            _playerPoseController = _context.GetPlayerContext()?.GetPlayerPoseContoroller;
            if (_playerPoseController != null)
            {
                // 専用メソッドを登録
                _playerPoseController.OnPoseStart += OnPlayerPoseStart;
            }

            _enemyContext = _context.GetEnemyContext();
            if (_enemyContext != null)
            {
                // 専用メソッドを登録
                _enemyContext.OnEnemyPoseStart += OnEnemyPoseStart;
            }
        }

        private void OnDestroy()
        {
            // イベントから登録解除
            if (_playerPoseController != null)
            {
                _playerPoseController.OnPoseStart -= OnPlayerPoseStart;
            }
            if (_enemyContext != null)
            {
                _enemyContext.OnEnemyPoseStart -= OnEnemyPoseStart;
            }
        }

        // プレイヤーからのイベント受付
        private void OnPlayerPoseStart(Pose.PoseID poseID)
        {
            bool wasCleared = JudgeRequest(poseID, true);

            if (!wasCleared)
            {
                return;
            }

            _textAnimationController?.PlayVoiceClearAnimation(poseID);
        }


        // 敵からのイベント受付
        private void OnEnemyPoseStart(Enemy.Enemy enemy, Pose.PoseID poseID)
       {
            JudgeRequest(poseID, false, enemy);
        }

        // 共通判定ロジック
        public bool JudgeRequest(Pose.PoseID poseID,bool isPlayer, Enemy.Enemy enemy = null)
        {
            int requestCount = _requestSystem.GetRequestCount(poseID);

            if (requestCount <= 0)
            {
                return false;
            }

            _requestSystem.ClearRequest(poseID, isPlayer, enemy);
            return true;
        }
    }
}
