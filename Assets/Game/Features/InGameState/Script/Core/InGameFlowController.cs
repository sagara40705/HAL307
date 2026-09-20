using System.Collections;
using UnityEngine;
using static Sound.Manager.SoundManager;

// インゲーム進行とWave進行の制御
public sealed class InGameFlowController : MonoBehaviour
{
    [SerializeField] private StageData stageData;
    [SerializeField] private GameModeStageDataSettings stageDataSettings;
    [SerializeField] private InGameFlowContext context;
    [SerializeField] private bool playOnStart = true;

    private Coroutine _flowCoroutine;
    private IInGameFlowState _currentState;
    private bool _isChangingState;
    private int _currentWaveIndex;
    private WaveData _currentWaveData;
    private int _currentTargetScore;
    private float _currentEnemyAnimationSpeed = 1f;

    public int CurrentWaveIndex => _currentWaveIndex;
    public WaveData CurrentWaveData => _currentWaveData;
    public string CurrentStateName => _currentState != null ? _currentState.StateName : "None";
    public bool IsFlowRunning => _flowCoroutine != null;
    public bool IsChangingState => _isChangingState;
    public int CurrentTargetScore => _currentTargetScore;
    public float CurrentEnemyAnimationSpeed => _currentEnemyAnimationSpeed;

    // Context未設定時の同一GameObject検索
    private void Awake()
    {
        if (context == null)
        {
            context = GetComponent<InGameFlowContext>();
        }
    }

    // 進行オブジェクト停止時に演出用モデルを復元
    private void OnDisable()
    {
        context?.RestoreWavePresentationStage();
    }

    // 自動開始設定時の進行開始
    private void Start()
    {
        if (!playOnStart)
        {
            return;
        }

        StartFlow();
        Sound.Manager.SoundManager.Play((int)SceneSoundTag.IntroSound, (int)IntroSoundTag.Handclap);
    }

    // インゲーム進行の開始
    public void StartFlow()
    {
        if (_flowCoroutine != null)
        {
            return;
        }

        ResolveStageData();

        if (stageData == null)
        {
            Debug.LogError("[InGameFlowController] StartFlow failed. Reason=StageData is not assigned.");
            return;
        }

        _flowCoroutine = StartCoroutine(FlowCoroutine());
    }

    private void ResolveStageData()
    {
        if (stageDataSettings == null)
        {
            return;
        }

        GameMode gameMode = GameMode.ThreePlayer;
        GameModeManager gameModeManager = GameModeManager.Instance;
        if (gameModeManager != null && gameModeManager.IsInitialized)
        {
            gameMode = gameModeManager.CurrentGameMode;
        }
        else
        {
            Debug.LogWarning("[InGameFlowController] GameModeManager is not ready. ThreePlayer StageData is used.");
        }

        StageData selectedStageData = stageDataSettings.GetStageDataOrNull(gameMode);
        if (selectedStageData == null)
        {
            Debug.LogWarning($"[InGameFlowController] StageDataSettings has no StageData for {gameMode}. Inspector StageData is used.");
            return;
        }

        stageData = selectedStageData;
        Debug.Log($"[InGameFlowController] StageData selected. Mode={gameMode}, StageData={stageData.name}");
    }

    // 指定Stateへの切り替え開始
    public void ChangeState(IInGameFlowState nextState)
    {
        if (_isChangingState)
        {
            Debug.LogWarning("[InGameFlowController] ChangeState skipped. Reason=State is changing.");
            return;
        }

        StartCoroutine(ChangeStateCoroutine(nextState));
    }

    // 標準のステージ進行
    private IEnumerator FlowCoroutine()
    {
        _currentWaveIndex = 0;
        GameResultManager.Instance?.BeginRun();

        var gameStartState = new GameStartFlowState(stageData);
        yield return ChangeStateCoroutine(gameStartState);
        while (!gameStartState.IsPresentationFinished)
        {
            yield return null;
        }

        _currentWaveData = stageData.GetWaveOrNull(_currentWaveIndex);
        _currentTargetScore = stageData.GetTargetScore(_currentWaveIndex);
        _currentEnemyAnimationSpeed = stageData.GetEnemyAnimationSpeed(_currentWaveIndex);

        while (_currentWaveData != null)
        {
            WaveData currentWave = _currentWaveData;
            yield return ChangeStateCoroutine(new PlayingFlowState(stageData, currentWave, _currentEnemyAnimationSpeed));

            int currentScore = context != null ? context.CurrentPlayerScore : 0;
            bool hasNextWave = stageData.HasWave(_currentWaveIndex + 1);
            var resultCheckState = new WaveResultCheckFlowState(CurrentTargetScore, currentScore, hasNextWave);
            yield return ChangeStateCoroutine(resultCheckState);

            if (resultCheckState.IsTargetScoreReached)
            {
                GameResultManager.Instance?.RecordWaveCleared(_currentWaveIndex + 1);
            }

            if (resultCheckState.Result != WaveResultCheckResult.NextWave)
            {
                break;
            }

            WaveData nextWave = stageData.GetWaveOrNull(_currentWaveIndex + 1);
            if (nextWave == null)
            {
                Debug.LogError($"[InGameFlowController] Next wave selection failed. WaveIndex={_currentWaveIndex + 1}, Endless={stageData.IsEndlessMode}");
                break;
            }

            // ウェーブ切り替え演出を実行
            yield return ChangeStateCoroutine(new WaveTransitionFlowState(currentWave, nextWave));
            _currentWaveIndex++;    // ウェーブを進める
            ScoreManager.Instance?.ResetAllScores();
            _currentWaveData = nextWave;
            _currentTargetScore = stageData.GetTargetScore(_currentWaveIndex);
            _currentEnemyAnimationSpeed = stageData.GetEnemyAnimationSpeed(_currentWaveIndex);
        }

        yield return ChangeStateCoroutine(new GameEndFlowState(stageData));

        _flowCoroutine = null;
    }

    // 現在StateのExitと次StateのEnter/Executeを実行
    private IEnumerator ChangeStateCoroutine(IInGameFlowState nextState)
    {
        if (nextState == null)
        {
            Debug.LogError("[InGameFlowController] ChangeState failed. Reason=Next state is null.");
            yield break;
        }

        _isChangingState = true;

        if (_currentState != null)
        {
            yield return StartCoroutine(_currentState.Exit(context));
        }

        _currentState = nextState;
        Debug.Log($"[InGameFlowController] Enter State={_currentState.StateName}");

        yield return StartCoroutine(_currentState.Enter(context));
        yield return StartCoroutine(_currentState.Execute(context));

        _isChangingState = false;
    }
}
