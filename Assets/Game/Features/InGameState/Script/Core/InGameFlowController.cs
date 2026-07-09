using System.Collections;
using UnityEngine;

// インゲーム進行とWave進行の制御
public sealed class InGameFlowController : MonoBehaviour
{
    [SerializeField] private StageData stageData;
    [SerializeField] private InGameFlowContext context;
    [SerializeField] private bool playOnStart = true;

    private Coroutine _flowCoroutine;
    private IInGameFlowState _currentState;
    private bool _isChangingState;
    private int _currentWaveIndex;

    public int CurrentWaveIndex => _currentWaveIndex;

    // Context未設定時の同一GameObject検索
    private void Awake()
    {
        if (context == null)
        {
            context = GetComponent<InGameFlowContext>();
        }
    }

    // 自動開始設定時の進行開始
    private void Start()
    {
        if (!playOnStart)
        {
            return;
        }

        StartFlow();
    }

    // インゲーム進行の開始
    public void StartFlow()
    {
        if (_flowCoroutine != null)
        {
            return;
        }

        if (stageData == null)
        {
            Debug.LogError("[InGameFlowController] StartFlow failed. Reason=StageData is not assigned.");
            return;
        }

        _flowCoroutine = StartCoroutine(FlowCoroutine());
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

        var gameStartState = new GameStartFlowState(stageData);
        yield return ChangeStateCoroutine(gameStartState);
        while (!gameStartState.IsPresentationFinished)
        {
            yield return null;
        }

        while (stageData.HasWave(_currentWaveIndex))
        {
            WaveData currentWave = stageData.GetWaveOrNull(_currentWaveIndex);
            yield return ChangeStateCoroutine(new PlayingFlowState(currentWave));

            int currentScore = context != null ? context.CurrentPlayerScore : 0;
            bool hasNextWave = stageData.HasWave(_currentWaveIndex + 1);
            var resultCheckState = new WaveResultCheckFlowState(currentWave, currentScore, hasNextWave);
            yield return ChangeStateCoroutine(resultCheckState);

            if (resultCheckState.Result != WaveResultCheckResult.NextWave)
            {
                break;
            }

            WaveData nextWave = stageData.GetWaveOrNull(_currentWaveIndex + 1);
            yield return ChangeStateCoroutine(new WaveTransitionFlowState(currentWave, nextWave));
            _currentWaveIndex++;
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
