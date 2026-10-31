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

    private bool _isFlowRunning;
    private IInGameFlowState _currentState;
    private bool _isChangingState;
    private int _currentWaveIndex;
    private WaveData _currentWaveData;
    private int _currentTargetScore;
    private float _currentEnemyAnimationSpeed = 1f;
    private EndlessRoundFlow _endlessFlow;
    private IEnumerator _normalFlowRoutine;

    public int CurrentWaveIndex => _currentWaveIndex;
    public WaveData CurrentWaveData => _currentWaveData;
    public string CurrentStateName => _endlessFlow != null ? _endlessFlow.Phase : _currentState != null ? _currentState.StateName : "None";
    public bool IsEndlessMode => stageData != null && stageData.IsEndlessMode;
    public bool IsFlowRunning => _isFlowRunning;
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
        if (_endlessFlow != null)
        {
            _endlessFlow.Dispose();
            StopAllCoroutines();
            _endlessFlow = null;
            _isFlowRunning = false;
        }
        else if (_normalFlowRoutine != null)
        {
            // Unityによる停止だけでは、入れ子のフェード待機のfinallyが実行されない場合がある。
            StopAllCoroutines();
            (_normalFlowRoutine as System.IDisposable)?.Dispose();
            _normalFlowRoutine = null;
            _isFlowRunning = false;
        }
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
        if (_isFlowRunning)
        {
            return;
        }

        ResolveStageData();

        if (stageData == null)
        {
            Debug.LogError("[InGameFlowController] StartFlow failed. Reason=StageData is not assigned.");
            return;
        }

        _isFlowRunning = true;
        IEnumerator routine = CoroutineUtility.Run(FlowCoroutine());
        if (!stageData.IsEndlessMode) { _normalFlowRoutine = routine; }
        StartCoroutine(routine);
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

        StartCoroutine(CoroutineUtility.Run(ChangeStateCoroutine(nextState)));
    }

    // 標準のステージ進行
    private IEnumerator FlowCoroutine()
    {
        try
        {
            _currentWaveIndex = 0;
            GameResultManager.Instance?.BeginRun();

            if (stageData.IsEndlessMode)
            {
                _endlessFlow = new EndlessRoundFlow(stageData, context, SetEndlessWave);
                try { yield return _endlessFlow.Run(); }
                finally
                {
                    _endlessFlow?.Dispose();
                    _endlessFlow = null;
                }
                yield break;
            }

            var gameStartState = new GameStartFlowState(stageData);
            yield return ChangeStateCoroutine(gameStartState);

            _currentWaveData = stageData.GetWaveOrNull(_currentWaveIndex);
            _currentTargetScore = stageData.GetTargetScore(_currentWaveIndex);
            _currentEnemyAnimationSpeed = stageData.GetEnemyAnimationSpeed(_currentWaveIndex);

            while (_currentWaveData != null)
            {
                WaveData currentWave = _currentWaveData;
                bool hasNextWave = stageData.HasWave(_currentWaveIndex + 1);
                WaveData nextWave = hasNextWave
                    ? stageData.GetWaveOrNull(_currentWaveIndex + 1)
                    : null;

                yield return ChangeStateCoroutine(new PlayingFlowState(
                    stageData,
                    currentWave,
                    _currentEnemyAnimationSpeed,
                    nextWave));

                int currentScore = context != null ? context.CurrentPlayerScore : 0;
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
        }
        finally
        {
            _isFlowRunning = false;
        }
    }

    private void SetEndlessWave(int index, WaveData wave)
    {
        _currentWaveIndex = index;
        _currentWaveData = wave;
        _currentTargetScore = stageData.GetTargetScore(index);
        _currentEnemyAnimationSpeed = stageData.GetEnemyAnimationSpeed(index);
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
        try
        {
            if (_currentState != null)
            {
                yield return _currentState.Exit(context);
            }

            _currentState = nextState;
            Debug.Log($"[InGameFlowController] Enter State={_currentState.StateName}");

            yield return _currentState.Enter(context);
            yield return _currentState.Execute(context);
        }
        finally
        {
            _isChangingState = false;
        }
    }
}
