using AudienceRequest;
using System.Collections;
using System.Collections.Generic;
using Enemy;
using UnityEngine;
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
    public int CurrentPlayerScore => ScoreSource != null ? ScoreSource.PlayerScore : 0;

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
    public IEnumerator PlayWavePresentation(IEnumerator presentation)
    {
        if (presentation == null)
        {
            yield break;
        }

        // ダンベルが落ち切って画面を覆うまで元のステージ表示を維持
        yield return TransitionManager.WaitForTransitionCompletion();

        ResolveStageModelContext();
        StageModelContext stage = stageModelContext;
        bool ownsOverride = stage != null && stage.BeginThreePlayerPresentation();
        if (ownsOverride)
        {
            _wavePresentationStage = stage;
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
    }

    public void ResetCameraPosition(StageData stageData)
    {
        if (stageData == null)
        {
            return;
        }

        Transform cameraTransform = ForwardProgressMover != null ? ForwardProgressMover.CameraTransform : null;
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
