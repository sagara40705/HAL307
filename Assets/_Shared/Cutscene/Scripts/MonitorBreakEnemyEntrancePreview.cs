using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

namespace Cutscene
{
    /// <summary>
    /// MonitorBreakの壁破壊後にEnemyを表示し、登場ポーズを再生します。
    /// </summary>
    public sealed class MonitorBreakEnemyEntrancePreview : MonoBehaviour
    {
        [Header("Cutscene")]
        [SerializeField] private GameObject[] _contentObjects;
        [SerializeField] private bool _playOnStart;
        [SerializeField] private bool _enablePreviewInput;
        [SerializeField] private bool _showPreviewLabel;

        [Header("Monitor Break")]
        [SerializeField] private MonitorBreakController _monitorBreakController;
        [SerializeField] private string _stageMonitorRendererName = "polySurface8";
        [SerializeField] private Vector3 _stageMonitorCenter = new Vector3(0f, 9.927004f, -32.05584f);
        [SerializeField] private Material _stageMonitorBreakMaterial;
        [SerializeField] private Material _stageMonitorGlowMaterial;
        [SerializeField] private bool _replaceStageMonitorMaterialOnBreak = true;
        [SerializeField] private Vector3 _breakWallPosition = new Vector3(0f, 9.927004f, -32f);
        [SerializeField] private Vector3 _breakWallRotationEuler = Vector3.zero;
        [SerializeField] private Vector3 _breakWallScale = new Vector3(22f, 13f, 1f);
        [SerializeField] private Vector3 _glowWallPosition = new Vector3(0f, 9.927004f, -32.02f);

        [Header("Enemy (World Space Fixed Positions)")]
        [SerializeField] private GameObject _enemyPrefab;
        [SerializeField] private AnimationClip _entrancePoseClip;
        [SerializeField] private Vector3 _enemyStartPosition = new Vector3(0f, 1.25f, -34f);
        [SerializeField] private Vector3 _enemyPosePosition = new Vector3(0f, 1.25f, -25f);
        [SerializeField] private Vector3 _enemyRotationEuler = Vector3.zero;
        [SerializeField] private Vector3 _enemyScale = Vector3.one;

        [Header("Timing")]
        [SerializeField, Min(0f)] private float _breakDelay = 1f;
        [SerializeField, Min(0f)] private float _enemyRevealDelay = 0.5f;
        [SerializeField, Min(0.01f)] private float _walkDuration = 5f;
        [SerializeField, Min(0f)] private float _posePlayDuration = 3f;

        private GameObject _enemy;
        private Animator _enemyAnimator;
        private RuntimeAnimatorController _originalAnimatorController;
        private Renderer _stageMonitorRenderer;
        private Material _stageMonitorRuntimeMaterial;
        private GameObject _breakWall;
        private GameObject _glowWall;
        private Renderer _breakTemplateRenderer;
        private Coroutine _sequence;
        private PlayableGraph _poseGraph;
        private AnimationClipPlayable _posePlayable;
        private bool _isEnding;

        private void Awake()
        {
            _breakTemplateRenderer = _monitorBreakController != null
                ? _monitorBreakController.GetComponent<Renderer>()
                : null;
            SetContentActive(false);
        }

        private void Start()
        {
            if (!CreateEnemy())
            {
                enabled = false;
                return;
            }

            PrepareEnemy();
            if (_playOnStart && IsStandalonePreview())
            {
                PlayFromStart();
            }
        }

        private void Update()
        {
            if (_enablePreviewInput && IsStandalonePreview() && Keyboard.current?.spaceKey.wasPressedThisFrame == true)
            {
                PlayFromStart();
            }
        }

        private void OnDestroy()
        {
            DestroyPoseGraph();
            RestoreStageMonitor();
        }

        private bool CreateEnemy()
        {
            if (_monitorBreakController == null || _enemyPrefab == null || _entrancePoseClip == null)
            {
                Debug.LogError(
                    $"[MonitorBreakEnemyEntrancePreview] Required references are not assigned. " +
                    $"MonitorBreakController={(_monitorBreakController != null)}, " +
                    $"EnemyPrefab={(_enemyPrefab != null)}, " +
                    $"EntrancePoseClip={(_entrancePoseClip != null)}",
                    this);
                return false;
            }

            _enemy = Instantiate(_enemyPrefab);
            SceneManager.MoveGameObjectToScene(_enemy, gameObject.scene);
            _enemy.name = "Entrance Enemy";
            _enemy.transform.SetPositionAndRotation(_enemyStartPosition, Quaternion.Euler(_enemyRotationEuler));
            _enemy.transform.localScale = _enemyScale;

            _enemyAnimator = _enemy.GetComponentInChildren<Animator>(true);
            if (_enemyAnimator == null)
            {
                Debug.LogError("[MonitorBreakEnemyEntrancePreview] Animator was not found.", _enemy);
                return false;
            }

            _originalAnimatorController = _enemyAnimator.runtimeAnimatorController;
            return true;
        }

        public void PlayFromStart()
        {
            _isEnding = false;
            SetContentActive(true);

            if (_enemy == null && !CreateEnemy())
            {
                // 依存アセットに問題があっても演出カメラまで消えた状態にはしない。
                return;
            }

            enabled = true;
            Replay();
        }

        public void Stop()
        {
            if (_sequence != null)
            {
                StopCoroutine(_sequence);
                _sequence = null;
            }

            DestroyPoseGraph();
            RestoreStageMonitor();
            _monitorBreakController?.ResetMonitor();
            PrepareEnemy();
            SetContentActive(false);
        }

        public void EndCutScene()
        {
            if (_isEnding)
            {
                return;
            }

            _isEnding = true;
            Stop();
            StartCoroutine(UnloadOwnerScene());
        }

        private void Replay()
        {
            if (_sequence != null)
            {
                StopCoroutine(_sequence);
            }

            DestroyPoseGraph();
            RestoreStageMonitor();
            _monitorBreakController.ResetMonitor();
            PrepareStageMonitorTarget();
            PrepareEnemy();
            _sequence = StartCoroutine(PlaySequence());
        }

        private void PrepareEnemy()
        {
            if (_enemy == null || _enemyAnimator == null)
            {
                return;
            }

            _enemy.transform.SetPositionAndRotation(_enemyStartPosition, Quaternion.Euler(_enemyRotationEuler));
            _enemyAnimator.runtimeAnimatorController = _originalAnimatorController;
            _enemyAnimator.Rebind();
            _enemy.SetActive(false);
        }

        private IEnumerator PlaySequence()
        {
            yield return new WaitForSeconds(_breakDelay);
            BreakStageMonitor();
            _monitorBreakController.BreakMonitor(GetBreakEffectPosition());

            yield return new WaitForSeconds(_enemyRevealDelay);
            _enemy.SetActive(true);

            _enemyAnimator.Play("Idle", 0, 0f);
            yield return MoveEnemyToPosePosition();

            PlayEntrancePose();

            yield return new WaitForSeconds(_posePlayDuration);
            FreezeEntrancePose();
            _sequence = null;
        }

        private IEnumerator MoveEnemyToPosePosition()
        {
            float elapsed = 0f;
            while (elapsed < _walkDuration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / _walkDuration);
                float easedTime = Mathf.SmoothStep(0f, 1f, normalizedTime);
                _enemy.transform.position = Vector3.Lerp(_enemyStartPosition, _enemyPosePosition, easedTime);
                yield return null;
            }

            _enemy.transform.position = _enemyPosePosition;
        }

        private void PlayEntrancePose()
        {
            _enemyAnimator.runtimeAnimatorController = null;
            _enemyAnimator.applyRootMotion = false;
            _enemyAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            _poseGraph = PlayableGraph.Create("Monitor Break Enemy Entrance Pose");
            _poseGraph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(_poseGraph, "Entrance Pose", _enemyAnimator);
            _posePlayable = AnimationClipPlayable.Create(_poseGraph, _entrancePoseClip);
            _posePlayable.SetApplyFootIK(true);
            output.SetSourcePlayable(_posePlayable);
            _poseGraph.Play();
        }

        private void FreezeEntrancePose()
        {
            if (!_poseGraph.IsValid() || !_posePlayable.IsValid())
            {
                return;
            }

            double finalPoseTime = Mathf.Max(0f, _entrancePoseClip.length - (1f / 60f));
            _posePlayable.SetTime(finalPoseTime);
            _posePlayable.SetSpeed(0d);
            _poseGraph.Evaluate(0f);
        }

        private void DestroyPoseGraph()
        {
            if (_poseGraph.IsValid())
            {
                _poseGraph.Destroy();
            }

            _posePlayable = default;
        }

        private void PrepareStageMonitorTarget()
        {
            if (!_replaceStageMonitorMaterialOnBreak || _stageMonitorBreakMaterial == null)
            {
                return;
            }

            ResolveStageMonitorRenderer();
            if (_stageMonitorRenderer != null && _breakTemplateRenderer != null)
            {
                _breakTemplateRenderer.enabled = false;
            }

        }

        private Vector3 GetBreakEffectPosition()
        {
            return _breakWallPosition;
        }

        private void BreakStageMonitor()
        {
            if (!_replaceStageMonitorMaterialOnBreak || _stageMonitorBreakMaterial == null)
            {
                return;
            }

            ResolveStageMonitorRenderer();
            if (_stageMonitorRenderer == null)
            {
                Debug.LogWarning("[MonitorBreakEnemyEntrancePreview] Stage monitor renderer was not found.", this);
                return;
            }

            _stageMonitorRuntimeMaterial = new Material(_stageMonitorBreakMaterial);
            CopyStageMonitorTexture(_stageMonitorRuntimeMaterial);
            _stageMonitorRuntimeMaterial.SetFloat("_BreakAmount", 1f);
            _breakWall = CreateStageWall(
                "Central Monitor Break Wall",
                _breakWallPosition,
                _breakWallRotationEuler,
                _breakWallScale,
                _stageMonitorRuntimeMaterial);
            _glowWall = CreateStageWall(
                "Central Monitor White Glow",
                _glowWallPosition,
                _breakWallRotationEuler,
                _breakWallScale,
                _stageMonitorGlowMaterial);
        }

        private GameObject CreateStageWall(
            string objectName,
            Vector3 position,
            Vector3 rotationEuler,
            Vector3 scale,
            Material material)
        {
            if (material == null)
            {
                Debug.LogWarning($"[MonitorBreakEnemyEntrancePreview] Material is missing. Object={objectName}", this);
                return null;
            }

            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wall.name = objectName;
            wall.transform.SetPositionAndRotation(position, Quaternion.Euler(rotationEuler));
            wall.transform.localScale = scale;
            SceneManager.MoveGameObjectToScene(wall, gameObject.scene);

            Collider wallCollider = wall.GetComponent<Collider>();
            if (wallCollider != null)
            {
                Destroy(wallCollider);
            }

            Renderer wallRenderer = wall.GetComponent<Renderer>();
            if (wallRenderer != null)
            {
                wallRenderer.sharedMaterial = material;
                wallRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                wallRenderer.receiveShadows = false;
            }

            return wall;
        }

        private void CopyStageMonitorTexture(Material destination)
        {
            if (destination == null || _stageMonitorRenderer == null)
            {
                return;
            }

            Material source = null;
            foreach (Material originalMaterial in _stageMonitorRenderer.sharedMaterials)
            {
                if (originalMaterial != null)
                {
                    source = originalMaterial;
                    break;
                }
            }

            if (source == null || !destination.HasProperty("_BaseMap"))
            {
                return;
            }

            const string baseMapProperty = "_BaseMap";
            const string mainTextureProperty = "_MainTex";
            string sourceProperty = source.HasProperty(baseMapProperty)
                ? baseMapProperty
                : source.HasProperty(mainTextureProperty)
                    ? mainTextureProperty
                    : null;

            if (sourceProperty == null)
            {
                return;
            }

            destination.SetTexture(baseMapProperty, source.GetTexture(sourceProperty));
            destination.SetTextureScale(baseMapProperty, source.GetTextureScale(sourceProperty));
            destination.SetTextureOffset(baseMapProperty, source.GetTextureOffset(sourceProperty));
        }

        private void RestoreStageMonitor()
        {
            if (_breakWall != null)
            {
                Destroy(_breakWall);
            }

            if (_glowWall != null)
            {
                Destroy(_glowWall);
            }

            if (_stageMonitorRuntimeMaterial != null)
            {
                Destroy(_stageMonitorRuntimeMaterial);
            }

            if (_breakTemplateRenderer != null)
            {
                _breakTemplateRenderer.enabled = true;
            }

            _breakWall = null;
            _glowWall = null;
            _stageMonitorRuntimeMaterial = null;
            _stageMonitorRenderer = null;
        }

        private void ResolveStageMonitorRenderer()
        {
            if (_stageMonitorRenderer != null)
            {
                return;
            }

            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            float bestSqrDistance = float.PositiveInfinity;
            Renderer fallbackRenderer = null;
            float fallbackSqrDistance = 4f;

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null ||
                    renderer.gameObject.scene == gameObject.scene ||
                    !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                float sqrDistance = (renderer.bounds.center - _stageMonitorCenter).sqrMagnitude;
                Vector3 size = renderer.bounds.size;
                if (sqrDistance < fallbackSqrDistance && size.x >= 30f && size.y >= 8f)
                {
                    fallbackSqrDistance = sqrDistance;
                    fallbackRenderer = renderer;
                }

                if (!string.Equals(renderer.name, _stageMonitorRendererName, System.StringComparison.Ordinal))
                {
                    continue;
                }

                if (sqrDistance >= bestSqrDistance)
                {
                    continue;
                }

                bestSqrDistance = sqrDistance;
                _stageMonitorRenderer = renderer;
            }

            _stageMonitorRenderer ??= fallbackRenderer;
        }

        private void SetContentActive(bool active)
        {
            if (_contentObjects == null)
            {
                return;
            }

            foreach (GameObject contentObject in _contentObjects)
            {
                if (contentObject != null && contentObject.activeSelf != active)
                {
                    contentObject.SetActive(active);
                }
            }
        }

        private bool IsStandalonePreview()
        {
            return gameObject.scene == SceneManager.GetActiveScene();
        }

        private IEnumerator UnloadOwnerScene()
        {
            Scene ownerScene = gameObject.scene;
            if (!ownerScene.IsValid() || !ownerScene.isLoaded)
            {
                yield break;
            }

            SceneLoader sceneLoader = SceneLoader.Instance;
            if (sceneLoader != null && sceneLoader.IsInitialized)
            {
                yield return sceneLoader.UnloadAsync(ownerScene.name);
                yield break;
            }

            if (SceneManager.sceneCount <= 1)
            {
                Debug.LogWarning($"[MonitorBreakEnemyEntrancePreview] Cannot unload the last scene. Scene='{ownerScene.name}'", this);
                yield break;
            }

            AsyncOperation operation = SceneManager.UnloadSceneAsync(ownerScene);
            if (operation != null)
            {
                yield return operation;
            }
        }

        private void OnGUI()
        {
            if (_showPreviewLabel && IsStandalonePreview())
            {
                GUI.Label(new Rect(16f, 16f, 620f, 24f), "Monitor Break -> Enemy Walk In -> Entrance Pose -> Freeze   Space: Replay");
            }
        }
    }
}
