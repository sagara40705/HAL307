using System.Collections;
using System.Collections.Generic;
using Pose;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace AudienceRequest
{
    public class AudienceRequestUiPool : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private AudienceRequestSystem _audienceRequestSystem; // AudienceRequestSystemの参照

        [SerializeField] private RectTransform _uiParent; // UIの親オブジェクト
        [SerializeField] private List<GameObject> _uiPrefabs = new(); // UIプレハブのリスト
        private WaveGaugeController _waveGaugeController;

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

        // ハートが消える瞬間
        [SerializeField] private GameObject _explosionPrefab;

        [Header("リクエストからハートへ変化する演出")]
        [SerializeField] private GameObject _heartChangeEffectPrefab;
        [SerializeField] private Vector3 _heartChangeEffectScale = Vector3.one;

        [Header("ハート表示時間")]
        [SerializeField, Min(0f)]
        private float _heartDisplayDuration = 0.5f;

        [Header("黒リクエストUI")]
        [SerializeField] private List<GameObject> _blackUiPrefabs = new();

        private List<ObjectPool<GameObject>> _blackUiPools = new();
        private List<List<GameObject>> _activeBlackUis = new();

        [System.Serializable]

        public class SpritePair
        {
            public PoseID poseID;
            public Sprite originalSprite;
            public Sprite changeSprite;
            public Sprite enemyChangeSprite;
        }

        [Header("スプライト変換テーブル")]
        [SerializeField] private List<SpritePair> _spriteTable = new();
        private readonly Dictionary<PoseID, SpritePair> _spriteTableByPose = new();

        private List<ObjectPool<GameObject>> _uiPools = new(); // UIオブジェクトプールのリスト
        private List<List<GameObject>> _activeUis = new(); // アクティブなUIオブジェクトのリスト

        // 
        private void Awake()
        {
            _waveGaugeController = FindFirstObjectByType<WaveGaugeController>();

            // スプライト変換テーブルを構築
            BuildSpriteLookup();

            // UIプールの初期化
            int capacity = _audienceRequestSystem.MaxRequestCount; // キャパはリクエストの最大合計

            // UIプレハブの数とPoseIDの数が一致しているか確認
            int count = _uiPrefabs.Count;
            if (count != (int)PoseID.Pose_Count)
            {
                Debug.LogError("UI Prefabs count does not match PoseID count.");
                return;
            }

            // UIプールの作成
            for (int i = 0; i < count; ++i)
            {
                int idx = i;
                ObjectPool<GameObject> pool = new ObjectPool<GameObject>(
                     () => Instantiate(_uiPrefabs[idx]),
                     obj => obj.SetActive(true),
                     obj =>
                     {
                         ResetUiVisual(obj, (PoseID)idx);
                         obj.SetActive(false);
                         obj.transform.SetParent(_uiParent.transform, false);
                         obj.transform.localScale = Vector3.one; // クリア時のスケールを維持

                     },
                     obj => Destroy(obj),
                     false, capacity
                    );
                _uiPools.Add(pool);
                _activeUis.Add(new List<GameObject>());
            }

            // 黒リクエストUI用プール作成
            if (_blackUiPrefabs.Count != (int)PoseID.Pose_Count)
            {
                Debug.LogError("Black UI Prefabs count does not match PoseID count.");
                return;
            }

            for (int i = 0; i < _blackUiPrefabs.Count; ++i)
            {
                int idx = i;

                ObjectPool<GameObject> blackPool = new ObjectPool<GameObject>(
                    () => Instantiate(_blackUiPrefabs[idx]),
                    obj => obj.SetActive(true),
                    obj =>
                    {
                        obj.SetActive(false);
                        obj.transform.SetParent(_uiParent.transform, false);
                        obj.transform.localScale = Vector3.one;
                    },
                    obj => Destroy(obj),
                    false,
                    1
                );

                _blackUiPools.Add(blackPool);
                _activeBlackUis.Add(new List<GameObject>());
            }

        }

        private void Start()
        {
            //_audienceRequestSystem.OnRequestReceived += Generate;
        }

        // 指定ポーズのUIを生成する
        public GameObject Generate(PoseID pose)
        {
            if (!CanGenerateUi())
            {
                return null;
            }

            if (!IsValidPose(pose))
            {
                Debug.LogError($"Invalid audience request pose: {pose}");
                return null;
            }

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

            Image img = obj.GetComponent<Image>();
            if (img != null)
            {
                if (TryGetSpritePair(pose, out SpritePair spriteSetting))
                {
                    img.sprite = spriteSetting.originalSprite;
                }
            }

            PlayShowPresentation(obj);

            _activeUis[(int)pose].Add(obj);
            return obj;
        }


        // 指定ポーズのUIを演出付きで消す
        public void PlayClearEffectAndRelease(PoseID pose, bool isPlayer)
        {
            Debug.Log($"[TimeScale Check] Time.timeScale = {Time.timeScale}");
            if (!IsValidPose(pose))
            {

                Debug.LogError($"Invalid audience request pose: {pose}");
                return;
            }

            List<GameObject> targetList = new List<GameObject>(_activeUis[(int)pose]);
            _activeUis[(int)pose].Clear();
            TryGetSpritePair(pose, out SpritePair spriteSetting);

            foreach (GameObject obj in targetList)
            {
                if (obj == null)
                {
                    continue;
                }

                Image img = obj.GetComponent<Image>();

                if (img != null && spriteSetting != null)
                {
                    // リクエスト画像をハート画像へ変更
                    img.sprite = isPlayer
                        ? spriteSetting.changeSprite
                        : spriteSetting.enemyChangeSprite;

                    // リクエストからハートへ変わった瞬間のエフェクト
                    SpawnEffect(_heartChangeEffectPrefab, obj,_heartChangeEffectScale,true);
                }

                // ハートを一定時間表示してからExplosionで消す
                StartCoroutine(PlayHeartThenRelease(obj,pose));
            }
        }



        public void ClearUiPool(PoseID pose)
        {
            if (!IsValidPose(pose))
            {
                Debug.LogError($"Invalid audience request pose: {pose}");
                return;
            }

            // アクティブなUIオブジェクトをすべてプールに返す
            foreach (var o in _activeUis[(int)pose])
            {
                _uiPools[(int)pose].Release(o);
            }

            // アクティブなUIオブジェクトのリストをクリア
            _activeUis[(int)pose].Clear();
        }

        public void ClearAllUiPools()
        {
            for (int i = 0; i < _activeUis.Count; i++)
            {
                List<GameObject> activeList = _activeUis[i];
                ObjectPool<GameObject> pool = _uiPools[i];

                foreach (GameObject obj in activeList)
                {
                    if (obj == null)
                    {
                        continue;
                    }

                    pool.Release(obj);
                }

                activeList.Clear();
            }
        }

        private IEnumerator PlayHeartThenRelease( GameObject obj,PoseID pose)
        {
            // ハートの姿を一定時間見せる
            if (_heartDisplayDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(_heartDisplayDuration);
            }

            if (obj == null)
            {
                yield break;
            }

            // ハートが消える瞬間に既存Explosionを再生
            SpawnEffect(_explosionPrefab,obj,Vector3.one,false);

            // ハートを消す
            PlayHidePresentation(obj);

            // AdjustableUiPresenterのHide演出を見せる時間
            if (_clearDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(_clearDelay);
            }

            if (obj == null)
            {
                yield break;
            }

            if (!IsValidPose(pose))
            {
                yield break;
            }

            int poseIndex = (int)pose;

            if (poseIndex < 0 || poseIndex >= _uiPools.Count)
            {
                Debug.LogError(
                    $"[AudienceRequestUiPool] UI Pool is missing for pose : {pose}"
                );
                yield break;
            }

            _uiPools[poseIndex].Release(obj);
        }

        private IEnumerator PlayHeartThenReleaseBlack(GameObject obj,PoseID pose)
        {
            if (_heartDisplayDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(_heartDisplayDuration);
            }

            if (obj == null)
            {
                yield break;
            }

            SpawnEffect(_explosionPrefab,obj,Vector3.one,false);

            PlayHidePresentation(obj);

            if (_clearDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(_clearDelay);
            }

            if (obj == null)
            {
                yield break;
            }

            if (!IsValidPose(pose))
            {
                yield break;
            }

            _blackUiPools[(int)pose].Release(obj);
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

        // 指定された座標が既存のUIオブジェクトと重なっているかどうかを判定する
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

            foreach (var list in _activeBlackUis)
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

        // 指定された座標が禁止エリア内にあるかどうかを判定する
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

        private void ResetUiVisual(GameObject obj, PoseID pose)
        {
            if (obj == null) return;

            Image img = obj.GetComponent<Image>();
            if (img != null && TryGetSpritePair(pose, out SpritePair spriteSetting))
            {
                img.sprite = spriteSetting.originalSprite;
            }

            obj.transform.localScale = Vector3.one;
        }

        private void BuildSpriteLookup()
        {
            _spriteTableByPose.Clear();

            foreach (var pair in _spriteTable)
            {
                if (pair == null)
                {
                    continue;
                }

                if (!IsValidPose(pair.poseID))
                {
                    Debug.LogError($"Audience request sprite table contains invalid pose: {pair.poseID}");
                    continue;
                }

                if (_spriteTableByPose.ContainsKey(pair.poseID))
                {
                    Debug.LogError($"Audience request sprite table contains duplicate pose: {pair.poseID}");
                    continue;
                }

                _spriteTableByPose.Add(pair.poseID, pair);
            }

            for (int i = 0; i < (int)PoseID.Pose_Count; i++)
            {
                PoseID pose = (PoseID)i;
                if (!_spriteTableByPose.ContainsKey(pose))
                {
                    Debug.LogError($"Audience request sprite table is missing pose: {pose}");
                }
            }
        }

        private bool TryGetSpritePair(PoseID pose, out SpritePair spritePair)
        {
            if (_spriteTableByPose.TryGetValue(pose, out spritePair))
            {
                return true;
            }

            Debug.LogError($"Audience request sprite table does not contain pose: {pose}");
            return false;
        }

        private static bool IsValidPose(PoseID pose)
        {
            int poseIndex = (int)pose;
            return poseIndex >= 0 && poseIndex < (int)PoseID.Pose_Count;
        }

        public GameObject GenerateInArea(PoseID pose, Vector2 areaMin, Vector2 areaMax)
        {
            if (!CanGenerateUi())
            {
                return null;
            }

            if (!IsValidPose(pose))
            {
                Debug.LogError($"Invalid audience request pose: {pose}");
                return null;
            }

            if (!TryFindSpawnPositionInArea(areaMin, areaMax, out Vector2 spawnPos))
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

            Image img = obj.GetComponent<Image>();

            if (img != null)
            {
                if (TryGetSpritePair(pose, out SpritePair spriteSetting))
                {
                    img.sprite = spriteSetting.originalSprite;
                }
            }

            PlayShowPresentation(obj);

            _activeUis[(int)pose].Add(obj);
            return obj;
        }

        private bool TryFindSpawnPositionInArea(Vector2 areaMin, Vector2 areaMax, out Vector2 pos)
        {
            for (int i = 0; i < _maxAttempts; i++)
            {
                Vector2 candidate = new Vector2(
                    Random.Range(areaMin.x, areaMax.x),
                    Random.Range(areaMin.y, areaMax.y)
                );

                if (IsInsideForbiddenArea(candidate))
                {
                    continue;
                }

                if (IsOverlapping(candidate))
                {
                    continue;
                }

                pos = candidate;
                return true;
            }

            pos = Vector2.zero;
            return false;
        }


        public GameObject GenerateBlack(PoseID pose)
        {
            if (!CanGenerateUi())
            {
                return null;
            }

            if (!IsValidPose(pose))
            {
                Debug.LogError($"Invalid black audience request pose: {pose}");
                return null;
            }

            if (!TryFindSpawnPosition(out Vector2 spawnPos))
            {
                return null;
            }

            GameObject obj = _blackUiPools[(int)pose].Get();

            obj.transform.SetParent(_uiParent.transform, false);
            obj.transform.localScale = Vector3.one;

            RectTransform rect = obj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = spawnPos;
            }

            PlayShowPresentation(obj);

            _activeBlackUis[(int)pose].Add(obj);
            return obj;
        }

        public void PlayBlackClearEffectAndRelease(PoseID pose)
        {
            if (!IsValidPose(pose))
            {
                Debug.LogError($"Invalid black audience request pose: {pose}");
                return;
            }

            List<GameObject> targetList = new List<GameObject>(_activeBlackUis[(int)pose]);
            _activeBlackUis[(int)pose].Clear();

            foreach (GameObject obj in targetList)
            {
                if (obj == null)
                {
                    continue;
                }

                // リクエストからハートへ変わった瞬間のエフェクト
                SpawnEffect(_heartChangeEffectPrefab, obj, _heartChangeEffectScale,true);

                // ハートを一定時間表示してからExplosionで消す
                StartCoroutine( PlayHeartThenReleaseBlack(obj, pose) );

            }
        }

        public void ClearAllBlackUiPools()
        {
            for (int i = 0; i < _activeBlackUis.Count; i++)
            {
                List<GameObject> activeList = _activeBlackUis[i];
                ObjectPool<GameObject> pool = _blackUiPools[i];

                foreach (GameObject obj in activeList)
                {
                    if (obj == null)
                    {
                        continue;
                    }

                    pool.Release(obj);
                }

                activeList.Clear();
            }
        }

        private void SpawnEffect(
         GameObject effectPrefab,
         GameObject target,
         Vector3 effectScale,
         bool showInFront)
        {
            if (IsFinalPoseActive())
            {
                return;
            }

            if (effectPrefab == null || target == null || _uiParent == null)
            {
                return;
            }

            GameObject effect = Instantiate(effectPrefab, _uiParent);

            RectTransform effectRect = effect.GetComponent<RectTransform>();
            RectTransform targetRect = target.GetComponent<RectTransform>();

            if (effectRect == null || targetRect == null)
            {
                return;
            }

            effectRect.anchoredPosition = targetRect.anchoredPosition;
            effectRect.localScale = effectScale;

            if (showInFront)
            {
                // ハートより手前へ表示
                effectRect.SetAsLastSibling();
            }
            else
            {
                // ハートのすぐ奥へ表示
                int targetIndex = targetRect.GetSiblingIndex();

                effectRect.SetSiblingIndex(
                    Mathf.Max(0, targetIndex - 1)
                );
            }
        }


        private void PlayShowPresentation(GameObject obj)
        {
            if (obj == null)
            {
                return;
            }

            if (!obj.activeSelf)
            {
                obj.SetActive(true);
            }

            AdjustableUiPresenter presenter =
                obj.GetComponent<AdjustableUiPresenter>();

            if (presenter == null)
            {
                return;
            }

            presenter.CaptureCurrentAsBase();
            presenter.Show();
        }

        private void PlayHidePresentation(GameObject obj)
        {
            if (obj == null || !obj.activeInHierarchy)
            {
                return;
            }

            AdjustableUiPresenter presenter =
                obj.GetComponent<AdjustableUiPresenter>();

            if (presenter == null)
            {
                return;
            }

            presenter.Hide();
        }


        private bool CanGenerateUi()
        {
            if (IsFinalPoseActive())
            {
                return false;
            }

            if (_uiParent == null)
            {
                Debug.LogWarning(
                    "[AudienceRequestUiPool] UI generation skipped. UI Parent is null."
                );

                return false;
            }

            return _uiParent.gameObject.activeInHierarchy;
        }

        private bool IsFinalPoseActive()
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

