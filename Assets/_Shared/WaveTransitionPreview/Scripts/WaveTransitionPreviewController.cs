using System.Collections;
using Enemy;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace WaveTransitionPreview
{
    /// <summary>
    /// 本番の加算シーンとWave用Stateを使い、Wave切り替え演出だけを再生します。
    /// InGameStateシーンは読み込まないため、通常のゲーム進行とは同時に動きません。
    /// </summary>
    public sealed class WaveTransitionPreviewController : MonoBehaviour
    {
        [Header("Wave")]
        [Tooltip("切り替え先として再生するWaveDataです。")]
        [SerializeField] private WaveData waveData;
        [Tooltip("切り替え元Waveの終了Actionを厳密に指定する場合に使用します。未設定時はWave Dataを使用します。")]
        [SerializeField] private WaveData completedWaveData;

        [Header("Production Setup")]
        [SerializeField] private GameModeStageDataSettings stageDataSettings;
        [SerializeField] private InGameFlowContext context;
        [SerializeField] private string[] productionSceneNames =
        {
            "StageModel",
            "Enemy",
            "Player",
            "InGameUI",
            "AudienceRequest",
            "crowdtest",
        };

        [Header("Preview")]
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private bool showGuide = true;

        private Coroutine _prepareCoroutine;
        private Coroutine _playCoroutine;
        private StageData _stageData;
        private bool _isReady;
        private bool _hasPlayed;
        private string _status = "Preparing production scenes...";

        public bool IsReady => _isReady;
        public bool IsPlaying => _playCoroutine != null;

        private void Awake()
        {
            if (context == null)
            {
                context = GetComponent<InGameFlowContext>();
            }
        }

        private void Start()
        {
            _prepareCoroutine = StartCoroutine(PrepareAndPlay());
        }

        private void Update()
        {
            if (Keyboard.current?.spaceKey.wasPressedThisFrame != true || !_isReady || IsPlaying)
            {
                return;
            }

            if (_hasPlayed)
            {
                ReloadPreviewScene();
                return;
            }

            Play();
        }

        private void OnDisable()
        {
            context?.RestoreWavePresentationStage();
        }

        public void Play()
        {
            if (!_isReady)
            {
                Debug.LogWarning("[WaveTransitionPreview] Play skipped. Preview is not ready.", this);
                return;
            }

            if (IsPlaying)
            {
                return;
            }

            if (waveData == null)
            {
                Debug.LogError("[WaveTransitionPreview] WaveData is not assigned.", this);
                return;
            }

            _playCoroutine = StartCoroutine(PlayWaveTransition());
        }

        private IEnumerator PrepareAndPlay()
        {
            if (context == null)
            {
                _status = "Setup failed. InGameFlowContext is missing.";
                Debug.LogError($"[WaveTransitionPreview] {_status}", this);
                _prepareCoroutine = null;
                yield break;
            }

            if (productionSceneNames != null)
            {
                foreach (string sceneName in productionSceneNames)
                {
                    if (string.IsNullOrWhiteSpace(sceneName) || IsSceneLoaded(sceneName))
                    {
                        continue;
                    }

                    _status = $"Loading {sceneName}...";
                    yield return LoadProductionScene(sceneName);
                    if (!IsSceneLoaded(sceneName))
                    {
                        _status = $"Scene load failed: {sceneName}";
                        Debug.LogError($"[WaveTransitionPreview] {_status}", this);
                        _prepareCoroutine = null;
                        yield break;
                    }
                }
            }

            // 加算シーン側のStartとContext登録を完了させる。
            yield return null;
            yield return null;

            if (!ResolveStageData() || !SetupProductionActors())
            {
                _prepareCoroutine = null;
                yield break;
            }

            context.ResetProgressFollowerPositions(_stageData);
            context.ForwardProgressMover?.StopMove();
            _isReady = true;
            _status = playOnStart ? "Playing wave transition..." : "Ready. Press Space to play.";
            _prepareCoroutine = null;

            if (playOnStart)
            {
                Play();
            }
        }

        private static IEnumerator LoadProductionScene(string sceneName)
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName))
            {
                Debug.LogError($"[WaveTransitionPreview] Scene is not in Build Settings. Scene={sceneName}");
                yield break;
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (operation == null)
            {
                Debug.LogError($"[WaveTransitionPreview] LoadSceneAsync returned null. Scene={sceneName}");
                yield break;
            }

            yield return operation;

            Scene loadedScene = SceneManager.GetSceneByName(sceneName);
            if (!loadedScene.IsValid() || !loadedScene.isLoaded)
            {
                yield break;
            }

            // 本番のSceneLoaderと同じく、加算シーン用Camera/Lightを停止する。
            foreach (GameObject root in loadedScene.GetRootGameObjects())
            {
                AdditiveSceneObjectDisabler disabler = root.GetComponentInChildren<AdditiveSceneObjectDisabler>(true);
                if (disabler == null)
                {
                    continue;
                }

                disabler.DisableTargets();
                break;
            }
        }

        private static bool IsSceneLoaded(string sceneName)
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            return scene.IsValid() && scene.isLoaded;
        }

        private bool ResolveStageData()
        {
            if (stageDataSettings == null)
            {
                _status = "StageDataSettings is not assigned.";
                Debug.LogError($"[WaveTransitionPreview] {_status}", this);
                return false;
            }

            GameMode gameMode = GameMode.ThreePlayer;
            GameModeManager gameModeManager = GameModeManager.Instance;
            if (gameModeManager != null && gameModeManager.IsInitialized)
            {
                gameMode = gameModeManager.CurrentGameMode;
            }

            _stageData = stageDataSettings.GetStageDataOrNull(gameMode);
            if (_stageData != null)
            {
                return true;
            }

            _status = $"StageData is missing for {gameMode}.";
            Debug.LogError($"[WaveTransitionPreview] {_status}", this);
            return false;
        }

        private bool SetupProductionActors()
        {
            EnemyLineupManager lineup = context.EnemyLineup;
            if (lineup == null)
            {
                _status = "EnemyLineupManager was not found.";
                Debug.LogError($"[WaveTransitionPreview] {_status}", this);
                return false;
            }

            EnemyMaxCount maxEnemyCount = EnemyMaxCount.Two;
            GameModeManager gameModeManager = GameModeManager.Instance;
            if (gameModeManager != null && gameModeManager.IsInitialized)
            {
                maxEnemyCount = (EnemyMaxCount)gameModeManager.MaxEnemyCount;
                gameModeManager.LockSelection();
            }

            lineup.SetupInitialEnemies(
                _stageData.InitialEnemies,
                _stageData.InitialEnemyCount,
                maxEnemyCount);
            return true;
        }

        private IEnumerator PlayWaveTransition()
        {
            _status = $"Playing: {waveData.name}";
            context.ForwardProgressMover?.StopMove();

            WaveData sourceWave = completedWaveData != null
                ? completedWaveData
                : FindCompletedWave(_stageData, waveData) ?? waveData;
            var transitionState = new WaveTransitionFlowState(sourceWave, waveData);
            yield return transitionState.Execute(context);

            float animationSpeed = FindWaveAnimationSpeed(_stageData, waveData);
            var playingState = new PlayingFlowState(_stageData, waveData, animationSpeed);
            yield return playingState.Enter(context);

            // プレビューでは演出後の通常プレイ進行を開始しない。
            context.ForwardProgressMover?.StopMove();
            _hasPlayed = true;
            _status = $"Completed: {waveData.name}   Space: reload and replay";
            _playCoroutine = null;
        }

        private static float FindWaveAnimationSpeed(StageData stageData, WaveData targetWave)
        {
            if (stageData == null || targetWave == null || stageData.IsEndlessMode)
            {
                return 1f;
            }

            for (int waveIndex = 0; waveIndex < stageData.WaveCount; waveIndex++)
            {
                if (stageData.GetWaveOrNull(waveIndex) == targetWave)
                {
                    return stageData.GetEnemyAnimationSpeed(waveIndex);
                }
            }

            return 1f;
        }

        private static WaveData FindCompletedWave(StageData stageData, WaveData targetWave)
        {
            if (stageData == null || targetWave == null || stageData.IsEndlessMode)
            {
                return null;
            }

            for (int waveIndex = 1; waveIndex < stageData.WaveCount; waveIndex++)
            {
                if (stageData.GetWaveOrNull(waveIndex) == targetWave)
                {
                    return stageData.GetWaveOrNull(waveIndex - 1);
                }
            }

            return null;
        }

        private void ReloadPreviewScene()
        {
            TransitionManager.ResetImmediate();
            Scene previewScene = gameObject.scene;
            if (!previewScene.IsValid())
            {
                return;
            }

            SceneManager.LoadScene(previewScene.name, LoadSceneMode.Single);
        }

        private void OnGUI()
        {
            if (!showGuide)
            {
                return;
            }

            GUI.Label(new Rect(16f, 16f, 760f, 24f), $"Wave Transition Preview | {_status}");
        }
    }
}
