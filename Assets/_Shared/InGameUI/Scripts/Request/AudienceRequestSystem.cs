using System;
using System.Collections.Generic;
using Pose;
using UnityEngine;

namespace AudienceRequest
{
    // 観客のリクエストを管理するシステム
    public class AudienceRequestSystem : MonoBehaviour
    {
        // チュートリアルモードかどうかのフラグ
        [SerializeField] private bool _isTutorialMode = false;

        // 最大リクエスト数の設定
        [SerializeField] private int _maxRequestCount = 3;

        // 最大リクエスト数を取得するプロパティ
        public int MaxRequestCount
        {
            get
            {
                if (_levelDesignSettings != null)
                {
                    return _levelDesignSettings.MaxRequestCount;
                }

                return _maxRequestCount;
            }
        }

        // 現在のリクエスト数を保持する変数
        [SerializeField] private int _currentRequestCount = 0;
        public int CurrentRequestCount => _currentRequestCount;

        // リクエストのデータベース、UIプール、レベルデザイン設定、コンテキストの参照
        [SerializeField] private AudienceRequests _audienceRequests; // リクエストのデータベース
        [SerializeField] private AudienceRequestUiPool _uiPool; // UIプールの参照
        [SerializeField] private AudienceRequestLevelDesignSettings _levelDesignSettings;
        [SerializeField] private AudienceRequestContext _context;

        private readonly List<PoseID> _normalRequestCandidates = new();
        private readonly List<PoseID> _blackRequestCandidates = new();

        // フィーバーゲージ
        [SerializeField] private FeverGaugeController _feverGaugeController;

        // リクエスト生成の間隔とタイマー
        [SerializeField] private float _requestInterval = 5f;
        private float _requestTimer = 0f;

        // リクエスト生成時のイベント
        public event Action<PoseID> OnRequestReceived;

        [Header("黒リクエスト")]
        [SerializeField] private float _blackRequestInterval = 15f;
        [SerializeField, Range(0f, 1f)] private float _blackRequestChance = 0.35f;

        // 黒リクエストのタイマーと状態
        private float _blackRequestTimer = 0f;
        private PoseID _activeBlackRequestPoseID = PoseID.None;

        // 黒リクエストがアクティブかどうかを判定するプロパティ
        public bool HasActiveBlackRequest => _activeBlackRequestPoseID != PoseID.None;

        //
        [Header("Wave状態")]
        [SerializeField]
        private WaveGaugeController _waveGaugeController;

        // 初期化
        private void Start()
        {
            ResetRequests();

            if (_feverGaugeController == null)
            {
                _feverGaugeController =
                    FindFirstObjectByType<FeverGaugeController>();
            }

            if (_context == null)
            {
                _context =
                    FindFirstObjectByType<AudienceRequestContext>();
            }

            if (_waveGaugeController == null)
            {
                _waveGaugeController =
                    FindFirstObjectByType<WaveGaugeController>();
            }

            if (_waveGaugeController == null)
            {
                Debug.LogWarning(
                    "[AudienceRequestSystem] " +
                    "WaveGaugeController was not found."
                );
            }
        }

        // 毎フレーム更新
        private void Update()
        {
            // チュートリアルモードの場合はリクエスト生成を行わない
            if (_isTutorialMode)
            {
                return;
            }

            if (InGameFlowLock.IsPresentationPlaying) { return; }

            // リクエストが満杯の場合は新しいリクエストを生成しない
            if (!FullRequest())
            {
                _requestTimer += Time.deltaTime;

                float interval = _levelDesignSettings != null
                    ? _levelDesignSettings.NormalRequestInterval
                    : _requestInterval;

                if (_requestTimer >= interval)
                {
                    _requestTimer = 0f;
                    GenerateRequest();
                }
            }

            // 黒リクエスト生成判定
            UpdateBlackRequestTimer();
        }

        // 指定Poseのリクエスト数を取得する関数
        public int GetRequestCount(PoseID poseID)
        {
            return _audienceRequests != null ? _audienceRequests.GetCount(poseID) : 0;
        }

        // 指定Poseが確定IDになるFakePoseをEnemyへ要求する
        public void RequestFakePoseForNextPose(PoseID committedPoseID)
        {
            if (_context == null)
            {
                _context = FindFirstObjectByType<AudienceRequestContext>();
            }

            Enemy.EnemyContext enemyContext = _context?.GetEnemyContext();
            Enemy.EnemyPoseController poseController = enemyContext?.GetEnemyPoseController();
            if (poseController == null)
            {
                Debug.LogWarning($"[AudienceRequestSystem] Fake pose request skipped. Reason=EnemyPoseController was not found. Pose={committedPoseID}");
                return;
            }

            // EnemyPoseControllerにFakePoseを要求する
            poseController.RequestFakePoseForNextPose(committedPoseID);
        }

        // リクエストをクリアする関数
        public void ResetRequests()
        {
            // リクエストのカウントをリセット
            _requestTimer = 0f;
            _blackRequestTimer = 0f;
            _activeBlackRequestPoseID = PoseID.None;

            _currentRequestCount = 0;
            _audienceRequests?.ClearAll();
            _uiPool?.ClearAllUiPools();
            _uiPool?.ClearAllBlackUiPools();
        }

        // リクエストをクリアする関数
        public void ClearRequest(PoseID poseID, bool isPlayer, Enemy.Enemy enemy = null)
        {
            int value = RemoveRequestCount(poseID);

            if (value <= 0)
            {
                return;
            }

            bool blockScoreReward = IsScoreRewardBlocked();

            if (isPlayer)
            {
                // フィーバー加算は今まで通り行う
                _feverGaugeController?.AddFill();

                // FinalPose中でなければスコアを加算
                if (!blockScoreReward &&
                    ScoreManager.Instance != null)
                {
                    bool isFever =_feverGaugeController != null &&_feverGaugeController.IsFever;

                    ScoreManager.Instance.AddPlayerScore(value,isFever);
                }
            }
            else
            {
                // FinalPose中でなければ敵スコアを加算
                if (!blockScoreReward &&ScoreManager.Instance != null && enemy != null)
                {
                    ScoreManager.Instance.AddEnemyScore(value,enemy.name);
                }
            }

            if (blockScoreReward)
            {
                Debug.Log(
                    $"[AudienceRequestSystem] " +
                    $"Score reward skipped during FinalPose. " +
                    $"Pose={poseID}, IsPlayer={isPlayer}, Count={value}"
                );
            }

            // ハート化や消去演出は実行する
            _uiPool?.PlayClearEffectAndRelease(poseID, isPlayer);
        }

        // リクエスト発生関数
        private void GenerateRequest()
        {
            if (InGameFlowLock.IsPresentationPlaying) { return; }

            if (FullRequest() || _audienceRequests == null || _uiPool == null) { return; }
            // 生成できるリクエストの数を計算
            int spawnCountPerWave = _levelDesignSettings != null
                 ? _levelDesignSettings.SpawnCountPerWave
                 : 3;

            int canSpawnCount = Mathf.Min(spawnCountPerWave, MaxRequestCount - _currentRequestCount);
            if (canSpawnCount <= 0) { return; }

            // ランダムにリクエストを生成
            _normalRequestCandidates.Clear();
            foreach (AudienceRequestEntry request in _audienceRequests.Requests)
            {
                if (request != null)
                {
                    _normalRequestCandidates.Add(request.poseID);
                }
            }
            if (_normalRequestCandidates.Count == 0) { return; }

            // 生成するリクエストの数だけループ
            for (int i = 0; i < canSpawnCount; i++)
            {
                // ランダムにリクエストを選ぶ
                PoseID poseID = GetRandomNormalRequestPose(_normalRequestCandidates);

                if (poseID == PoseID.None)
                {
                    continue;
                }

                // 先にUI生成を試す
                if (!RegisterGeneratedRequest(poseID, _uiPool.Generate(poseID)))
                {
                    continue;
                }
                Debug.Log("New request received! Current request count: " + _currentRequestCount);
            }
        }

        // リクエストが満杯かどうかをチェックする関数
        private bool FullRequest()
        {
            return _currentRequestCount >= MaxRequestCount;
        }

        // チュートリアルモードの設定
        public void SetTutorialMode(bool isTutorial)
        {
            _isTutorialMode = isTutorial;
            _requestTimer = 0f;
        }

        public void ShowTutorialRequest(PoseID poseID)
        {
            if (!CanShowTutorialRequest(poseID))
            {
                return;
            }

            // UI生成を試す
            if (!RegisterGeneratedRequest(poseID, _uiPool.Generate(poseID)))
            {
                return;
            }

            // デバッグログを出力
            Debug.Log("[Tutorial] Show tutorial request: " + poseID);
        }

        // チュートリアルモードのリクエストをクリアする関数
        public void ClearTutorialRequest(PoseID poseID, bool isPlayer)
        {
            ClearRequest(poseID, isPlayer);
        }

        // チュートリアルモードのリクエストをクリアする関数
        public void ClearTutorialRequestWithoutReward(PoseID poseID, bool isPlayer)
        {
            if (RemoveRequestCount(poseID) <= 0)
            {
                return;
            }

            // スコア加算・フィーバー加算はしない
            // UIの演出だけ再生して消す
            _uiPool?.PlayClearEffectAndRelease(poseID, isPlayer);
        }

        // チュートリアルモードのリクエストをすべてクリアする関数
        public void ClearAllTutorialRequests()
        {
            _uiPool?.ClearAllUiPools();
            _audienceRequests?.ClearAll();
            _currentRequestCount = 0;
        }

        // チュートリアルモードのリクエストを指定範囲内に表示する関数
        public void ShowTutorialRequestInArea(PoseID poseID, Vector2 areaMin, Vector2 areaMax)
        {
            if (!CanShowTutorialRequest(poseID))
            {
                return;
            }

            if (!RegisterGeneratedRequest(poseID, _uiPool.GenerateInArea(poseID, areaMin, areaMax)))
            {
                return;
            }

            Debug.Log("[Tutorial] Show tutorial request in area: " + poseID);
        }

        // 黒リクエストの生成タイマーを更新する関数
        private void UpdateBlackRequestTimer()
        {
            // すでに黒リクエストが出ているなら、新しく出さない
            if (_activeBlackRequestPoseID != PoseID.None)
            {
                return;
            }

            // 通常リクエストが1個もないなら、黒リクエストは出さない
            if (_currentRequestCount <= 0)
            {
                return;
            }

            // 黒リクエストの生成タイマーを更新
            _blackRequestTimer += Time.deltaTime;

            // レベルデザイン設定がある場合は、そこから黒リクエストの生成間隔と確率を取得
            float blackInterval = _levelDesignSettings != null
              ? _levelDesignSettings.BlackRequestInterval
              : _blackRequestInterval;

            // 黒リクエストの生成確率を取得
            float blackChance = _levelDesignSettings != null
                ? _levelDesignSettings.BlackRequestChance
                : _blackRequestChance;

            // 黒リクエストの生成間隔に達していない場合は、何もしない
            if (_blackRequestTimer < blackInterval)
            {
                return;
            }

            _blackRequestTimer = 0f;

            // 黒リクエストの生成確率に基づいて、黒リクエストを生成するかどうかを判定
            if (UnityEngine.Random.value > blackChance)
            {
                return;
            }

            //
            TryGenerateBlackRequestFromActiveRequests();
        }

        // 黒リクエストを通常リクエストから生成する関数
        private void TryGenerateBlackRequestFromActiveRequests()
        {
            if (_audienceRequests == null || _uiPool == null)
            {
                return;
            }

            _blackRequestCandidates.Clear();

            //
            foreach (AudienceRequestEntry request in _audienceRequests.Requests)
            {
                if (request == null)
                {
                    continue;
                }

                // 今出ていない通常リクエストは候補にしない
                if (request.count <= 0)
                {
                    continue;
                }

                // Enemy側がFakePoseとして指定できるものだけ候補にする
                if (!IsFakeRequestablePose(request.poseID))
                {
                    continue;
                }

                _blackRequestCandidates.Add(request.poseID);
            }

            //
            if (_blackRequestCandidates.Count == 0)
            {
                return;
            }

            //
            PoseID selectedPoseID = GetRandomBlackRequestPose(_blackRequestCandidates);

            if (selectedPoseID == PoseID.None)
            {
                return;
            }

            GameObject blackUi = _uiPool.GenerateBlack(selectedPoseID);

            if (blackUi == null)
            {
                return;
            }

            _activeBlackRequestPoseID = selectedPoseID;

            Debug.Log("[AudienceRequestSystem] Black request generated: " + selectedPoseID);
        }

        // 黒リクエストとして指定できるPoseIDかどうかを判定する関数
        private static bool IsFakeRequestablePose(PoseID poseID)
        {
            return poseID == PoseID.Pose_Yaa ||
                   poseID == PoseID.Pose_Power ||
                   poseID == PoseID.Pose_Muscle;
        }

        // 黒リクエストをクリアして、対応する通常リクエストのPoseIDをEnemyへ渡す関数
        public void ClearBlackRequestAndRequestFakePose()
        {
            if (_activeBlackRequestPoseID == PoseID.None)
            {
                Debug.LogWarning("[AudienceRequestSystem] Black request was not found.");
                return;
            }

            PoseID committedPoseID = _activeBlackRequestPoseID;

            // 先に黒リクエストUIを消す
            _uiPool.PlayBlackClearEffectAndRelease(committedPoseID);

            // 黒リクエスト状態を解除
            _activeBlackRequestPoseID = PoseID.None;

            // 消えた黒リクエストに対応する普通リクエストのPoseIDをEnemyへ渡す
            RequestFakePoseForNextPose(committedPoseID);

            Debug.Log("[AudienceRequestSystem] Black request cleared. Fake pose committed ID = " + committedPoseID);
        }

        private bool CanShowTutorialRequest(PoseID poseID)
        {
            return !FullRequest() && _audienceRequests != null && _uiPool != null &&
                   _audienceRequests.Contains(poseID) && GetRequestCount(poseID) == 0;
        }

        // UI生成に成功したリクエストだけを登録する。
        private bool RegisterGeneratedRequest(PoseID poseID, GameObject ui)
        {
            if (ui == null)
            {
                return false;
            }

            _audienceRequests.AddCount(poseID);
            _currentRequestCount++;
            OnRequestReceived?.Invoke(poseID);
            return true;
        }

        private int RemoveRequestCount(PoseID poseID)
        {
            int count = GetRequestCount(poseID);
            if (count <= 0)
            {
                return 0;
            }

            _audienceRequests.ClearCount(poseID);
            _currentRequestCount = Mathf.Max(0, _currentRequestCount - count);
            return count;
        }

        // 通常・黒リクエストとも重み付き抽選を設定データ側に集約する。
        private PoseID GetRandomNormalRequestPose(List<PoseID> candidates)
        {
            if (candidates.Count == 0)
            {
                return PoseID.None;
            }

            PoseID poseID = _levelDesignSettings != null
                ? _levelDesignSettings.GetRandomNormalPoseFromCandidates(candidates)
                : PoseID.None;
            return poseID != PoseID.None
                ? poseID
                : candidates[UnityEngine.Random.Range(0, candidates.Count)];
        }

        // ランダムに黒リクエストのPoseIDを取得する関数
        private PoseID GetRandomBlackRequestPose(List<PoseID> candidates)
        {
            //
            if (candidates == null || candidates.Count == 0)
            {
                return PoseID.None;
            }

            if (_levelDesignSettings == null)
            {
                int index = UnityEngine.Random.Range(0, candidates.Count);
                return candidates[index];
            }

            PoseID poseID = _levelDesignSettings.GetRandomBlackPoseFromCandidates(candidates);

            if (poseID == PoseID.None)
            {
                int index = UnityEngine.Random.Range(0, candidates.Count);
                return candidates[index];
            }

            return poseID;
        }

        private bool IsScoreRewardBlocked()
        {
            if (_waveGaugeController == null)
            {
                _waveGaugeController =
                    FindFirstObjectByType<WaveGaugeController>();
            }

            return _waveGaugeController != null &&
                   _waveGaugeController.IsFinalPoseVisible;
        }

    }
}
