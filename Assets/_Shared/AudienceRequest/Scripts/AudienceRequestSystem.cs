using System;
using UnityEngine;

namespace AudienceRequest
{
    // 観客のリクエストを管理するシステム
    public class AudienceRequestSystem : MonoBehaviour
    {
        [SerializeField] private int _maxRequestCount = 3;
        [SerializeField] private int _currentRequestCount = 0;

        [SerializeField] private float _requestInterval = 5f;
        private float _requestTimer = 0f;

        // リクエスト生成時のイベント
        public event Action OnRequestReceived;

        private void Start()
        {
            _requestTimer = 0f;
            _currentRequestCount = 0;
        }

        private void Update()
        {
            if (FullRequest()) { return; }

            _requestTimer += Time.deltaTime;
            if (_requestTimer >= _requestInterval)
            {
                _requestTimer = 0f;
                GenerateRequest();
                Debug.Log("New request received! Current request count: " + _currentRequestCount);
            }
        }

        // リクエスト発生関数
        private void GenerateRequest()
        {
            if (FullRequest()) { return; }

            _currentRequestCount++;

            // 

            OnRequestReceived?.Invoke();
            Debug.Log("New request received! Current request count: " + _currentRequestCount);
        }

        private bool FullRequest()
        {
            return _currentRequestCount >= _maxRequestCount;
        }
    }
}
