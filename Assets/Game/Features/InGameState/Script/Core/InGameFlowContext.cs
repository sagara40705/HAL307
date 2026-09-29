using AudienceRequest;
using System.Collections;
using System.Collections.Generic;
using Enemy;
using UnityEngine;
using UnityEngine.SceneManagement;
using static Sound.Manager.SoundManager;

// インゲーム進行Stateから参照するシーン要素
public sealed class InGameFlowContext : MonoBehaviour
{
    [SerializeField] private string stageModelContextName = "StageModelContext";
    [SerializeField] private StageModelContext stageModelContext;
    [SerializeField] private ForwardProgressMover forwardProgressMover;
    [SerializeField] private StageEndJudge stageEndJudge;
    [SerializeField] private ScoreManager scoreManager;
    [SerializeField] private EnemyLineupManager enemyLineupManager;
    [SerializeField] private InGamePresentationEventRegistry presentationEventRegistry;
    [SerializeField] private WaveGaugeController waveGaugeController;
    [SerializeField] private InGamePresentationAction resetPositionAction;
    [SerializeField] private AudienceRequestSystem audienceRequestSystem;
    private StageModelContext _wavePresentationStage;
    private bool _isScenePreloading;
    private string _preloadedSceneName;
    private readonly Dictionary<GameObject, bool> _preloadedSceneRootStates = new();

    private const int ScenePreloadPriority = -10;

    // 前進制御
    public ForwardProgressMover ForwardProgressMover
    {
        get
        {
            ResolveForwardProgressMover();
            return forwardProgressMover;
        }
    }

    // ステージ終了判定
    public StageEndJudge StageEndJudge
    {
        get
        {
            ResolveStageModelContext();
            return stageEndJudge != null ? stageEndJudge : stageModelContext?.StageEndJudge;
        }
    }

    // ステージ終了判定の位置
    public Vector3 StageEndJudgePosition
    {
        get
        {
            ResolveStageModelContext();
            return stageEndJudge != null ? stageEndJudge.transform.position : stageModelContext != null ? stageModelContext.StageEndJudgePosition : Vector3.zero;
        }
    }

    // Wave終了エリアの位置
    public Vector3 WaveEndAreaPosition
    {
        get
        {
            ResolveStageModelContext();
            return stageModelContext != null ? stageModelContext.WaveEndAreaPosition : Vector3.zero;
        }
    }

    // スコア参照元
    public ScoreManager ScoreSource
    {
        get
        {
            ResolveScoreManager();
            return scoreManager;
        }
    }

    // 現在のプレイヤースコア
    public int CurrentPlayerScore
    {
        get
        {
            ScoreManager source = ScoreSource;
            return source != null ? source.PlayerScore : 0;
        }
    }

    public EnemyLineupManager EnemyLineup
    {
        get
        {
            ResolveEnemyLineupManager();
            return enemyLineupManager;
        }
    }

    public InGamePresentationEventRegistry PresentationEventRegistry
    {
        get
        {
            ResolvePresentationEventRegistry();
            return presentationEventRegistry;
        }
    }

    public WaveGaugeController WaveGauge
    {
        get
        {
            ResolveWaveGaugeController();
            return waveGaugeController;
        }
    }

    public InGamePresentationAction ResetPositionAction => resetPositionAction;

    public AudienceRequestSystem AudienceRequest
    {
        get
        {
            ResolveAudienceRequestSystem();
            return audienceRequestSystem;
        }
    }

    // Wave演出中だけ3Pモデルを表示。入れ子の演出例外でも復元処理を実行
    public IEnumerator PlayWavePresentation(
        IEnumerator presentation,
        bool revealTransitionCover = false)
    {
        if (presentation == null)
        {
            yield break;
        }

        BeginWavePresentationUi();

        // ダンベルが落ち切って画面を覆うまで元のステージ表示を維持
        try
        {
            yield return TransitionManager.WaitForTransitionCompletion();

            if (!revealTransitionCover)
            {
                TransitionManager.HoldBlackCover();
            }

            ResolveStageModelContext();
            StageModelContext stage = stageModelContext;
            bool ownsOverride = stage != null && stage.BeginThreePlayerPresentation();
            if (ownsOverride)
            {
                _wavePresentationStage = stage;
            }

            if (revealTransitionCover)
            {
                // 黒背景のフェードが指定位置まで進んだら、ダンベル落下中に演出を重ねて開始する
                yield return TransitionManager.RevealIfCovered();
            }

            var routines = new Stack<IEnumerator>();
            routines.Push(presentation);
            try
            {
                while (routines.Count > 0)
                {
                    IEnumerator current = routines.Peek();
                    if (!current.MoveNext())
                    {
                        routines.Pop();
                        (current as System.IDisposable)?.Dispose();
                    }
                    else if (current.Current is IEnumerator nested)
                    {
                        routines.Push(nested);
                    }
                    else
                    {
                        yield return current.Current;
                    }
                }

                // 演出から開始したフェードも完了後にモデルを復元
                yield return TransitionManager.WaitForTransitionCompletion();
            }
            finally
            {
                try
                {
                    while (routines.Count > 0)
                    {
                        (routines.Pop() as System.IDisposable)?.Dispose();
                    }
                }
                finally
                {
                    if (ownsOverride)
                    {
                        RestoreWavePresentationStage();
                    }
                }
            }
        }
        finally
        {
            EndWavePresentationUi();
        }
    }

    private void BeginWavePresentationUi()
    {
        WaveGauge?.BeginWavePresentation();
        AudienceRequest?.ResetRequests();
    }

    private void EndWavePresentationUi()
    {
        AudienceRequest?.ResetRequests();
        WaveGauge?.EndWavePresentation();
    }

    // 演出終了・進行停止・シーン解放時のモデル復元
    public void RestoreWavePresentationStage()
    {
        StageModelContext stage = _wavePresentationStage;
        _wavePresentationStage = null;
        if (stage != null)
        {
            stage.EndThreePlayerPresentation();
        }
    }

    private void OnDisable()
    {
        RestoreWavePresentationStage();
        RestorePreloadedSceneRoots();
    }

    public void BeginScenePreload(WaveData waveData)
    {
        if (waveData == null || !waveData.ShouldPreloadScene)
        {
            return;
        }

        string sceneName = waveData.PreloadSceneName;
        if (string.Equals(_preloadedSceneName, sceneName, System.StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (_isScenePreloading)
        {
            Debug.LogWarning(
                $"[InGameFlowContext] Scene preload skipped. Another preload is running. " +
                $"Current='{_preloadedSceneName}', Requested='{sceneName}'");
            return;
        }

        StartCoroutine(CoroutineUtility.Run(PreloadSceneCoroutine(sceneName)));
    }

    public bool IsScenePreloadTracked(string sceneName)
    {
        return !string.IsNullOrWhiteSpace(sceneName) &&
               string.Equals(_preloadedSceneName, sceneName, System.StringComparison.OrdinalIgnoreCase);
    }

    public IEnumerator ActivatePreloadedScene(string sceneName)
    {
        if (!IsScenePreloadTracked(sceneName))
        {
            yield break;
        }

        while (_isScenePreloading)
        {
            yield return null;
        }

        Scene scene = SceneLookup.Find(sceneName);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning($"[InGameFlowContext] Preloaded scene is not available. Scene='{sceneName}'");
            ClearScenePreloadState();
            yield break;
        }

        RestorePreloadedSceneRoots();
        ClearScenePreloadState();
    }

    private IEnumerator PreloadSceneCoroutine(string sceneName)
    {
        _isScenePreloading = true;
        try
        {
            yield return PreloadSceneCore(sceneName);
        }
        finally
        {
            _isScenePreloading = false;
        }
    }

    private IEnumerator PreloadSceneCore(string sceneName)
    {
        if (!string.IsNullOrWhiteSpace(_preloadedSceneName) &&
            !string.Equals(_preloadedSceneName, sceneName, System.StringComparison.OrdinalIgnoreCase))
        {
            string previousSceneName = _preloadedSceneName;

            Scene previousScene = SceneLookup.Find(previousSceneName);
            if (previousScene.IsValid() && previousScene.isLoaded)
            {
                SceneLoader sceneLoader = SceneLoader.Instance;
                if (sceneLoader != null && sceneLoader.IsInitialized)
                {
                    yield return sceneLoader.UnloadAsync(previousSceneName);
                }
                else
                {
                    AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(previousScene);
                    if (unloadOperation != null)
                    {
                        yield return unloadOperation;
                    }
                }
            }

            previousScene = SceneLookup.Find(previousSceneName);
            if (previousScene.IsValid() && previousScene.isLoaded)
            {
                Debug.LogWarning(
                    $"[InGameFlowContext] Scene preload skipped. Previous preloaded scene could not be unloaded. " +
                    $"Previous='{previousSceneName}', Requested='{sceneName}'");
                yield break;
            }
        }

        _preloadedSceneName = sceneName;
        _preloadedSceneRootStates.Clear();

        Scene scene = SceneLookup.Find(sceneName);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            SceneLoader sceneLoader = SceneLoader.Instance;
            if (sceneLoader != null && sceneLoader.IsInitialized)
            {
                yield return sceneLoader.LoadAdditiveAsync(sceneName, ScenePreloadPriority);
            }
            else if (Application.CanStreamedLevelBeLoaded(sceneName))
            {
                AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                if (operation != null)
                {
                    operation.priority = ScenePreloadPriority;
                    yield return operation;
                }
            }
            else
            {
                Debug.LogWarning($"[InGameFlowContext] Scene preload skipped. Scene cannot be loaded. Scene='{sceneName}'");
            }

            scene = SceneLookup.Find(sceneName);
        }

        if (scene.IsValid() && scene.isLoaded)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root == null)
                {
                    continue;
                }

                _preloadedSceneRootStates[root] = root.activeSelf;
                root.SetActive(false);
            }

            Debug.Log($"[InGameFlowContext] Scene preload completed. Scene='{sceneName}', Priority={ScenePreloadPriority}");
        }
        else
        {
            Debug.LogWarning($"[InGameFlowContext] Scene preload failed. Scene='{sceneName}'");
            ClearScenePreloadState();
        }
    }

    private void RestorePreloadedSceneRoots()
    {
        foreach (KeyValuePair<GameObject, bool> pair in _preloadedSceneRootStates)
        {
            if (pair.Key != null)
            {
                pair.Key.SetActive(pair.Value);
            }
        }

        _preloadedSceneRootStates.Clear();
    }

    private void ClearScenePreloadState()
    {
        _preloadedSceneName = null;
        _isScenePreloading = false;
        _preloadedSceneRootStates.Clear();
    }

    public void ResetCameraPosition(StageData stageData)
    {
        if (stageData == null)
        {
            return;
        }

        ForwardProgressMover mover = ForwardProgressMover;
        Transform cameraTransform = mover != null ? mover.CameraTransform : null;
        if (cameraTransform == null)
        {
            Debug.LogWarning("[InGameFlowContext] Camera transform was not found.");
            return;
        }

        cameraTransform.position = stageData.CameraInitialPosition;
    }

    public void ResetProgressFollowerPositions(StageData stageData)
    {
        ResetCameraPosition(stageData);
        ForwardProgressMover?.ResetStageEndJudgePosition();
        Sound.Manager.SoundManager.Play((int)SceneSoundTag.InGameSound, (int)InGameSoundTag.InGameBGM);
    }

    private void Awake()
    {
        ResolveStageModelContext();
        ResolveForwardProgressMover();
        ResolveScoreManager();
        ResolveEnemyLineupManager();
        ResolvePresentationEventRegistry();
        ResolveWaveGaugeController();
        ResolveAudienceRequestSystem();
    }

    // Inspector未設定時のForwardProgressMover検索
    private void ResolveForwardProgressMover()
    {
        if (forwardProgressMover != null)
        {
            return;
        }

        forwardProgressMover = FindFirstObjectByType<ForwardProgressMover>();
    }

    // Inspector未設定時のStageModelContext検索
    private void ResolveStageModelContext()
    {
        if (stageModelContext != null)
        {
            return;
        }

        ContextManager manager = ContextManager.Instance;
        if (manager != null && !string.IsNullOrWhiteSpace(stageModelContextName))
        {
            stageModelContext = manager.Get<StageModelContext>(stageModelContextName);
        }

        if (stageModelContext == null)
        {
            stageModelContext = FindFirstObjectByType<StageModelContext>();
        }
    }

    // Inspector未設定時のScoreManager検索
    private void ResolveScoreManager()
    {
        if (scoreManager != null)
        {
            return;
        }

        scoreManager = global::ScoreManager.Instance;
        if (scoreManager == null)
        {
            scoreManager = FindFirstObjectByType<ScoreManager>();
        }
    }

    // Inspector未設定時のEnemyLineupManager検索
    private void ResolveEnemyLineupManager()
    {
        if (enemyLineupManager != null)
        {
            return;
        }

        enemyLineupManager = FindFirstObjectByType<EnemyLineupManager>();
    }

    // Inspector譛ｪ險ｭ螳壽凾縺ｮInGamePresentationEventRegistry讀懃ｴ｢
    private void ResolvePresentationEventRegistry()
    {
        if (presentationEventRegistry != null)
        {
            return;
        }

        ResolveStageModelContext();
        presentationEventRegistry = stageModelContext != null ? stageModelContext.PresentationEventRegistry : null;
        if (presentationEventRegistry == null)
        {
            presentationEventRegistry = FindFirstObjectByType<InGamePresentationEventRegistry>();
        }
    }

    private void ResolveWaveGaugeController()
    {
        if (waveGaugeController != null)
        {
            return;
        }

        waveGaugeController = FindFirstObjectByType<WaveGaugeController>();
    }

    private void ResolveAudienceRequestSystem()
    {
        if (audienceRequestSystem != null)
        {
            return;
        }

        audienceRequestSystem = FindFirstObjectByType<AudienceRequestSystem>();
    }
}
