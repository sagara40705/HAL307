using System;
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

        [SerializeField] private float _requestInterval = 5f;
        private float _requestTimer = 0f;

        // リクエスト生成時のイベント
        public event Action<PoseID> OnRequestReceived;

        // 初期化
        private void Start()
        {
            _requestTimer = 0f;
            _currentRequestCount = 0;
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
        public void ClearRequest(PoseID poseID)
        {
            int value = _audienceRequests.GetCount(poseID);
            if (value <= 0) return;

            // リクエストのカウントを減らす
            _audienceRequests.ClearCount(poseID);

            // 合計リクエスト数を減らす
            _currentRequestCount -= value;
            if(_currentRequestCount < 0)
                _currentRequestCount = 0;

            _uiPool.PlayClearEffectAndRelease(poseID);
        }

        // リクエスト発生関数
        private void GenerateRequest()
        {
            if (FullRequest()) { return; }

            // ランダムにリクエストを生成
            AudienceRequestEntry requestEntry = _audienceRequests.GetRandomRequest();
            if (requestEntry == null) { return; }

            // 生成されたリクエストのカウントを増やす
            PoseID poseID = requestEntry.poseID;
            _audienceRequests.AddCount(poseID);

            // 合計リクエスト数をカウント
            _currentRequestCount++;

            // UIプールから対応するUIを生成
            GameObject ui = _uiPool.Generate(poseID);


            // リクエスト生成イベントを発火
            OnRequestReceived?.Invoke(poseID);
            Debug.Log("New request received! Current request count: " + _currentRequestCount);
        }

        // リクエストが満杯かどうかをチェックする関数
        private bool FullRequest()
        {
            return _currentRequestCount >= _maxRequestCount;
        }
    }
}
