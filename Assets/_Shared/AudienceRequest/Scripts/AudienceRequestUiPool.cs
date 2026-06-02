using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Pool;
using Pose;
using System.Collections;
using UnityEngine.UI;

namespace AudienceRequest
{
    public class AudienceRequestUiPool : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private AudienceRequestSystem _audienceRequestSystem; // AudienceRequestSystemの参照

        [SerializeField] private RectTransform _uiParent; // UIの親オブジェクト
        [SerializeField] private List<GameObject> _uiPrefabs = new(); // UIプレハブのリスト

        [Header("ランダム配置設定")]
        [SerializeField] private Vector2 _spawnAreaMin = new Vector2(50f, 50f); // ランダム配置の最小座標
        [SerializeField] private Vector2 _spawnAreaMax = new Vector2(600f, 300f);// ランダム配置の最大座標

        [Header("生成禁止エリア")]
        [SerializeField] private List<RectTransform> _forbiddenAreas = new();
        [SerializeField] private float _forbiddenPadding = 20f;

        [SerializeField] private float _minDistance = 80;
        [SerializeField] private int _maxAttempts = 10;

        [SerializeField] private float _clearScale = 1.5f;
        [SerializeField] private float _clearDelay = 1.0f;

        [System.Serializable]
        public class SpritePair
        {
            public Sprite originalSprite;
            public Sprite changeSprite;
        }

        [Header("スプライト変換テーブル")]
        [SerializeField] private List<SpritePair> _spriteTable = new();

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
                     obj =>
                     {
                         obj.SetActive(false);
                         obj.transform.SetParent(_uiParent.transform, false);
                         obj.transform.localScale = Vector3.one;

                     },
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
            if (!TryFindSpawnPosition(out Vector2 spawnPos))
            {
                return null;
            }

            GameObject obj = _uiPools[(int)pose].Get();
            obj.transform.SetParent(_uiParent.transform, false);
            obj.transform.localScale = Vector3.one;

            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = spawnPos;
            }

            _activeUis[(int)pose].Add(obj);
            return obj;
        }


        // 指定ポーズのUIを演出付きで消す
        public void PlayClearEffectAndRelease(PoseID pose)
        {
            List<GameObject> targetList = new List<GameObject>(_activeUis[(int)pose]);
            _activeUis[(int)pose].Clear();

            foreach (var obj in targetList)
            {
                if (obj == null) continue;

                Image img = obj.GetComponent<Image>();
                if (img != null && img.sprite != null)
                {
                    foreach (var pair in _spriteTable)
                    {
                        if (img.sprite == pair.originalSprite)
                        {
                            img.sprite = pair.changeSprite;
                            break;
                        }
                    }
                }

                obj.transform.localScale = Vector3.one * _clearScale;
                StartCoroutine(ReleaseAfterDelay(obj, pose, _clearDelay));
            }
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

        private IEnumerator ReleaseAfterDelay(GameObject obj, PoseID pose, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (obj != null)
            {
                _uiPools[(int)pose].Release(obj);
            }
        }


        private bool TryFindSpawnPosition(out Vector2 pos)
        {
            for (int i = 0; i < _maxAttempts; i++)
            {
                Vector2 candidate = new Vector2(
                    Random.Range(_spawnAreaMin.x, _spawnAreaMax.x),
                    Random.Range(_spawnAreaMin.y, _spawnAreaMax.y)
                );

                if (IsInsideForbiddenArea(candidate))
                    continue;

                if (IsOverlapping(candidate))
                    continue;

                pos = candidate;
                return true;
            }

            pos = Vector2.zero;
            return false;
        }

        private bool IsOverlapping(Vector2 pos)
        {
            foreach (var list in _activeUis)
            {
                foreach (var obj in list)
                {
                    if (obj == null || !obj.activeInHierarchy) continue;

                    RectTransform rect = obj.GetComponent<RectTransform>();
                    if (rect == null) continue;

                    if (Vector2.Distance(rect.anchoredPosition, pos) < _minDistance)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        private bool IsInsideForbiddenArea(Vector2 localPos)
        {
            foreach (var area in _forbiddenAreas)
            {
                if (area == null) continue;

                Vector3[] corners = new Vector3[4];
                area.GetWorldCorners(corners);

                Vector2 min = _uiParent.InverseTransformPoint(corners[0]);
                Vector2 max = _uiParent.InverseTransformPoint(corners[2]);

                min -= Vector2.one * _forbiddenPadding;
                max += Vector2.one * _forbiddenPadding;

                if (localPos.x >= min.x && localPos.x <= max.x &&
                    localPos.y >= min.y && localPos.y <= max.y)
                {
                    return true;
                }
            }

            return false;
        }

    }


}

