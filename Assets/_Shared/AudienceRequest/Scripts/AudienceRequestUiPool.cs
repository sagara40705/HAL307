using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Pool;
using Pose;

namespace AudienceRequest
{
    public class AudienceRequestUiPool : MonoBehaviour
    {
        [SerializeField] private AudienceRequestSystem _audienceRequestSystem; // AudienceRequestSystemの参照

        [SerializeField] private GameObject _uiParent; // UIの親オブジェクト
        [SerializeField] private List<GameObject> _uiPrefabs = new(); // UIプレハブのリスト

        private List<ObjectPool<GameObject>> _uiPools = new(); // UIオブジェクトプールのリスト
        private List<List<GameObject>> _activeUis = new(); // アクティブなUIオブジェクトのリスト

        private void Awake()
        {
            int capacity = _audienceRequestSystem.MaxRequestCount; // キャパはリクエストの最大合計

            int count = _uiPrefabs.Count;
            if (count != (int)PoseID.Pose_Count)
            {
                Debug.LogError("UI Prefabs count does not match PoseID count.");
                return;
            }

            for (int i = 0; i < count; ++i)
            {
                int idx = i;
                ObjectPool<GameObject> pool = new ObjectPool<GameObject>(
                     () => Instantiate(_uiPrefabs[idx]),
                     obj => obj.SetActive(true),
                     obj => obj.SetActive(false),
                     obj => Destroy(obj),
                     false, capacity
                    );
                _uiPools.Add(pool);
                _activeUis.Add(new List<GameObject>());
            }
        }

        private void Start()
        {
            //_audienceRequestSystem.OnRequestReceived += Generate;
        }

        public GameObject Generate(PoseID pose)
        {
            // プールからUIオブジェクトを取得
            GameObject obj = _uiPools[(int)pose].Get();

            // UIの親オブジェクトに設定
            obj.transform.SetParent(_uiParent.transform, false);

            // アクティブなUIオブジェクトのリストに追加
            _activeUis[(int)pose].Add(obj);

            return obj;
        }

        public void ClearUiPool(PoseID pose)
        {
            // アクティブなUIオブジェクトをすべてプールに返す
            foreach (var o in _activeUis[(int)pose])
            {
                _uiPools[(int)pose].Release(o);
            }

            // アクティブなUIオブジェクトのリストをクリア
            _activeUis[(int)pose].Clear();
        }
    }
}
