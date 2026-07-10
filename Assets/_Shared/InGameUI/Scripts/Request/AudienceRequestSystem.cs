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
        public int MaxRequestCount => _maxRequestCount;

        [SerializeField] private int _currentRequestCount = 0;
        public int CurrentRequestCount => _currentRequestCount;

        [SerializeField] private AudienceRequests _audienceRequests; // リクエストのデータベース
        [SerializeField] private AudienceRequestUiPool _uiPool; // UIプールの参照

        // フィーバーゲージ
        [SerializeField] private FeverGaugeController _feverGaugeController;

        [SerializeField] private float _requestInterval = 5f;
        private float _requestTimer = 0f;

        // リクエスト生成時のイベント
        public event Action<PoseID> OnRequestReceived;

        // 初期化
        private void Start()
        {
            ResetRequests();

            if (_feverGaugeController == null)
            {
                _feverGaugeController = FindFirstObjectByType<FeverGaugeController>();
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
            if (FullRequest()) { return; }

            // 一定時間ごとにリクエストを生成する
            _requestTimer += Time.deltaTime;
            if (_requestTimer >= _requestInterval)
            {
                _requestTimer = 0f;
                GenerateRequest();
                Debug.Log("New request received! Current request count: " + _currentRequestCount);
            }
        }

        public int GetRequestCount(PoseID poseID)
        {
            return _audienceRequests.GetCount(poseID);
        }

        // リクエストをクリアする関数
        public void ResetRequests()
        {
            _requestTimer = 0f;
            _currentRequestCount = 0;
            _audienceRequests?.ClearAll();
            _uiPool?.ClearAllUiPools();
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
            int canSpawnCount = Mathf.Min(3, _maxRequestCount - _currentRequestCount);
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
                int randomIndex = UnityEngine.Random.Range(0, availableRequests.Count);
                AudienceRequestEntry requestEntry = availableRequests[randomIndex];
                availableRequests.RemoveAt(randomIndex);

                // もしリクエストがnullならスキップ
                if (requestEntry == null) { continue; }

                // 
                PoseID poseID = requestEntry.poseID;

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
            return _currentRequestCount >= _maxRequestCount;
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
    }
}
