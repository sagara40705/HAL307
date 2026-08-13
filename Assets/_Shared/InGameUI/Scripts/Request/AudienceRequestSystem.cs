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

        [SerializeField] private int _maxRequestCount = 3;

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

        [SerializeField] private int _currentRequestCount = 0;
        public int CurrentRequestCount => _currentRequestCount;

        [SerializeField] private AudienceRequests _audienceRequests; // リクエストのデータベース
        [SerializeField] private AudienceRequestUiPool _uiPool; // UIプールの参照
        [SerializeField] private AudienceRequestLevelDesignSettings _levelDesignSettings;
        [SerializeField] private AudienceRequestContext _context;

        // フィーバーゲージ
        [SerializeField] private FeverGaugeController _feverGaugeController;

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

        // 初期化
        private void Start()
        {
            ResetRequests();

            if (_feverGaugeController == null)
            {
                _feverGaugeController = FindFirstObjectByType<FeverGaugeController>();
            }

            if (_context == null)
            {
                _context = FindFirstObjectByType<AudienceRequestContext>();
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


        public int GetRequestCount(PoseID poseID)
        {
            return _audienceRequests.GetCount(poseID);
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

            poseController.RequestFakePoseForNextPose(committedPoseID);
        }

        // リクエストをクリアする関数
        public void ResetRequests()
        {
            _requestTimer = 0f;
            _blackRequestTimer = 0f;
            _activeBlackRequestPoseID = PoseID.None;

            _currentRequestCount = 0;
            _audienceRequests?.ClearAll();
            _uiPool?.ClearAllUiPools();
            _uiPool?.ClearAllBlackUiPools();
        }

        public void ClearRequest(PoseID poseID, bool isPlayer, Enemy.Enemy enemy = null)
        {
            int value = _audienceRequests.GetCount(poseID);
            if (value <= 0) return;

            _audienceRequests.ClearCount(poseID);

            _currentRequestCount -= value;
            if (_currentRequestCount < 0)
                _currentRequestCount = 0;

            if (isPlayer)
            {
                _feverGaugeController?.AddFill();

                //
                if (ScoreManager.Instance != null)
                {
                    bool isFever = _feverGaugeController != null && _feverGaugeController.IsFever;
                    ScoreManager.Instance.AddPlayerScore(value, isFever);
                }
            }
            else
            {
                //
                if (ScoreManager.Instance != null && enemy != null)
                {
                    ScoreManager.Instance.AddEnemyScore(value, enemy.name);
                }
            }

            _uiPool.PlayClearEffectAndRelease(poseID, isPlayer);
        }

        // リクエスト発生関数
        private void GenerateRequest()
        {
            if (InGameFlowLock.IsPresentationPlaying) { return; }

            if (FullRequest()) { return; }
            // 生成できるリクエストの数を計算
            int spawnCountPerWave = _levelDesignSettings != null
                 ? _levelDesignSettings.SpawnCountPerWave
                 : 3;

            int canSpawnCount = Mathf.Min(spawnCountPerWave, MaxRequestCount - _currentRequestCount);
            if (canSpawnCount <= 0) { return; }

            // ランダムにリクエストを生成
            List<AudienceRequestEntry> availableRequests = new List<AudienceRequestEntry>(_audienceRequests.Requests);
            if (availableRequests.Count == 0) { return; }

            // 生成するリクエストの数だけループ
            for (int i = 0; i < canSpawnCount; i++)
            {
                // 生成できるリクエストがなくなったらループを抜ける
                if (availableRequests.Count == 0) { break; }

                // ランダムにリクエストを選ぶ
                PoseID poseID = GetRandomNormalRequestPose(availableRequests);

                if (poseID == PoseID.None)
                {
                    continue;
                }

                AudienceRequestEntry requestEntry = availableRequests.Find(r => r.poseID == poseID);

                if (requestEntry == null)
                {
                    continue;
                }

                availableRequests.Remove(requestEntry);

                // 先にUI生成を試す
                GameObject ui = _uiPool.Generate(poseID);
                if (ui == null)
                {
                    continue;
                }

                // UI生成に成功した時だけカウントを増やす
                _audienceRequests.AddCount(poseID);
                _currentRequestCount++;

                OnRequestReceived?.Invoke(poseID);
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
            // チュートリアルモードでない場合は何もしない
            if (FullRequest())
            {
                return;
            }

            // すでに同じリクエストが存在する場合は何もしない
            if (_audienceRequests.GetCount(poseID) > 0)
            {
                return;
            }

            // UI生成を試す
            GameObject ui = _uiPool.Generate(poseID);

            // UI生成に失敗した場合は何もしない
            if (ui == null)
            {
                return;
            }

            // UI生成に成功した場合はカウントを増やす
            _audienceRequests.AddCount(poseID);
            _currentRequestCount++;

            // リクエスト生成時のイベントを発火
            OnRequestReceived?.Invoke(poseID);

            // デバッグログを出力
            Debug.Log("[Tutorial] Show tutorial request: " + poseID);
        }

        public void ClearTutorialRequest(PoseID poseID, bool isPlayer)
        {
            ClearRequest(poseID, isPlayer);
        }

        public void ClearTutorialRequestWithoutReward(PoseID poseID, bool isPlayer)
        {
            int value = _audienceRequests.GetCount(poseID);

            if (value <= 0)
            {
                return;
            }

            _audienceRequests.ClearCount(poseID);

            _currentRequestCount -= value;

            if (_currentRequestCount < 0)
            {
                _currentRequestCount = 0;
            }

            // スコア加算・フィーバー加算はしない
            // UIの演出だけ再生して消す
            _uiPool.PlayClearEffectAndRelease(poseID, isPlayer);
        }

        public void ClearAllTutorialRequests()
        {
            for (int i = 0; i < (int)PoseID.Pose_Count; i++)
            {
                PoseID poseID = (PoseID)i;
                _uiPool.ClearUiPool(poseID);
            }

            _audienceRequests.ClearAll();
            _currentRequestCount = 0;
        }

        public void ShowTutorialRequestInArea(PoseID poseID, Vector2 areaMin, Vector2 areaMax)
        {
            if (FullRequest())
            {
                return;
            }

            if (_audienceRequests.GetCount(poseID) > 0)
            {
                return;
            }

            GameObject ui = _uiPool.GenerateInArea(poseID, areaMin, areaMax);

            if (ui == null)
            {
                return;
            }

            _audienceRequests.AddCount(poseID);
            _currentRequestCount++;

            OnRequestReceived?.Invoke(poseID);

            Debug.Log("[Tutorial] Show tutorial request in area: " + poseID);
        }

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

            _blackRequestTimer += Time.deltaTime;

            float blackInterval = _levelDesignSettings != null
              ? _levelDesignSettings.BlackRequestInterval
              : _blackRequestInterval;

            float blackChance = _levelDesignSettings != null
                ? _levelDesignSettings.BlackRequestChance
                : _blackRequestChance;

            if (_blackRequestTimer < blackInterval)
            {
                return;
            }

            _blackRequestTimer = 0f;

            if (UnityEngine.Random.value > blackChance)
            {
                return;
            }

            TryGenerateBlackRequestFromActiveRequests();
        }

        private void TryGenerateBlackRequestFromActiveRequests()
        {
            List<PoseID> candidates = new List<PoseID>();

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

                candidates.Add(request.poseID);
            }

            if (candidates.Count == 0)
            {
                return;
            }

            PoseID selectedPoseID = GetRandomBlackRequestPose(candidates);

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

        private static bool IsFakeRequestablePose(PoseID poseID)
        {
            return poseID == PoseID.Pose_Yaa ||
                   poseID == PoseID.Pose_Power ||
                   poseID == PoseID.Pose_Muscle;
        }

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

        private PoseID GetRandomNormalRequestPose(List<AudienceRequestEntry> availableRequests)
        {
            if (_levelDesignSettings == null)
            {
                if (availableRequests == null || availableRequests.Count == 0)
                {
                    return PoseID.None;
                }

                int randomIndex = UnityEngine.Random.Range(0, availableRequests.Count);
                return availableRequests[randomIndex].poseID;
            }

            List<RequestSpawnWeight> filteredWeights = new();

            foreach (RequestSpawnWeight weight in _levelDesignSettings.NormalRequestWeights)
            {
                if (weight == null)
                {
                    continue;
                }

                if (!weight.IsEnabled)
                {
                    continue;
                }

                if (weight.Weight <= 0)
                {
                    continue;
                }

                bool exists = availableRequests.Exists(r => r.poseID == weight.PoseID);

                if (!exists)
                {
                    continue;
                }

                filteredWeights.Add(weight);
            }

            if (filteredWeights.Count == 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, availableRequests.Count);
                return availableRequests[randomIndex].poseID;
            }

            int totalWeight = 0;

            foreach (RequestSpawnWeight weight in filteredWeights)
            {
                totalWeight += weight.Weight;
            }

            int randomValue = UnityEngine.Random.Range(0, totalWeight);
            int current = 0;

            foreach (RequestSpawnWeight weight in filteredWeights)
            {
                current += weight.Weight;

                if (randomValue < current)
                {
                    return weight.PoseID;
                }
            }

            return PoseID.None;
        }

        private PoseID GetRandomBlackRequestPose(List<PoseID> candidates)
        {
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
    }
}
