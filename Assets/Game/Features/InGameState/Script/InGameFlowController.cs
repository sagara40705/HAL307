using System.Collections;
using UnityEngine;

// Controls in-game flow and wave progression.
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

    // Finds context on the same GameObject when unset.
    private void Awake()
    {
        if (context == null)
        {
            context = GetComponent<InGameFlowContext>();
        }
    }

    // Starts flow automatically when enabled.
    private void Start()
    {
        if (!playOnStart)
        {
            return;
        }

        StartFlow();
    }

    // Starts the in-game flow.
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

    // Starts transition to the specified state.
    public void ChangeState(IInGameFlowState nextState)
    {
        if (_isChangingState)
        {
            Debug.LogWarning("[InGameFlowController] ChangeState skipped. Reason=State is changing.");
            return;
        }

        StartCoroutine(ChangeStateCoroutine(nextState));
    }

    // Standard stage flow.
    private IEnumerator FlowCoroutine()
    {
        _currentWaveIndex = 0;

        yield return ChangeStateCoroutine(new GameStartFlowState(stageData));

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

            yield return ChangeStateCoroutine(new WaveTransitionFlowState(currentWave));
            _currentWaveIndex++;
        }

        yield return ChangeStateCoroutine(new GameEndFlowState(stageData));

        _flowCoroutine = null;
    }

    // Executes current Exit, next Enter, and next Execute.
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
