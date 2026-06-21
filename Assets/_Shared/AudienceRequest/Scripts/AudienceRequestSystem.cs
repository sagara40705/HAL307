using System;
using System.Collections.Generic;
using Pose;
using UnityEngine;

namespace AudienceRequest
{
    // 観客のリクエストを管理するシステム
    public class AudienceRequestSystem : MonoBehaviour
    {
        [SerializeField] private int _maxRequestCount = 3;
        public int MaxRequestCount => _maxRequestCount;

        [SerializeField] private int _currentRequestCount = 0;
        public int CurrentRequestCount => _currentRequestCount;

        [SerializeField] private AudienceRequests _audienceRequests; // リクエストのデータベース
        [SerializeField] private AudienceRequestUiPool _uiPool; // UIプールの参照

        // リクエストのUIを表示するためのマテリアルと、リクエストが増えるごとにUIがどれだけ満たされるかの割合
        [SerializeField] private Material _flameMaterial;
        [SerializeField] private float _fillIncrease = 0.05f;

        [SerializeField] private float _requestInterval = 5f;
        private float _requestTimer = 0f;

        // リクエスト生成時のイベント
        public event Action<PoseID> OnRequestReceived;

        // 初期化
        private void Start()
        {
            _requestTimer = 0f;
            _currentRequestCount = 0;
            _audienceRequests.ClearAll();
        }

        // 毎フレーム更新
        private void Update()
        {
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
                float fill = _flameMaterial.GetFloat("_FillAmount");
                fill += _fillIncrease;
                _flameMaterial.SetFloat("_FillAmount", Mathf.Clamp01(fill));

                //
                if (ScoreManager.Instance != null)
                {
                    ScoreManager.Instance.AddPlayerScore(value);
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
    }
}
