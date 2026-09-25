using Enemy;
using Player;
using UnityEngine;

namespace AudienceRequest
{
    public class AudienceRequestJudge : MonoBehaviour
    {
        [SerializeField] private AudienceRequestContext _context;
        [SerializeField] private AudienceRequestSystem _requestSystem;
        [Header("Wave演出")]
        [SerializeField]
        private WaveGaugeController _waveGaugeController;

        [Header("音声クリア時のテキスト演出")]
        [SerializeField]
        private AudienceRequestTextAnimationController _textAnimationController;

        private PlayerPoseContoroller _playerPoseController;
        private EnemyContext _enemyContext;

        private void Start()
        {
            // PlayerPoseControllerを取得
            _playerPoseController = _context.GetPlayerContext()?.GetPlayerPoseContoroller;
            if (_playerPoseController != null)
            {
                // 専用メソッドを登録
                _playerPoseController.OnPoseStart += OnPlayerPoseStart;
            }

            // EnemyContextを取得
            _enemyContext = _context.GetEnemyContext();
            if (_enemyContext != null)
            {
                // 専用メソッドを登録
                _enemyContext.OnEnemyPoseStart += OnEnemyPoseStart;
            }

            // WaveGaugeControllerがInspectorに設定されていない場合は、シーン内から取得
            if (_waveGaugeController == null)
            {
                _waveGaugeController =
                    FindFirstObjectByType<WaveGaugeController>();
            }

            // WaveGaugeControllerが見つからなかった場合は警告を出す
            if (_waveGaugeController == null)
            {
                Debug.LogWarning(
                    "[AudienceRequestJudge] " +
                    "WaveGaugeController was not found."
                );
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

        private void OnPlayerPoseStart(Pose.PoseID poseID)
        {
            int requestCount =
                _requestSystem.GetRequestCount(poseID);

            if (requestCount <= 0)
            {
                return;
            }

            // リクエストを消す
            JudgeRequest(poseID, true);

            // テキストアニメーション
            _textAnimationController
                ?.PlayVoiceClearAnimation(poseID);

            // FinalPose中なら、その場でWave終了判定を発火
            _waveGaugeController
                ?.CompleteWaveOnPlayerRequestClear();
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
