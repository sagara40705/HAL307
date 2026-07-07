using System.Collections;
using UnityEngine;

public sealed class InGameFlowController : MonoBehaviour
{
    [SerializeField] private InGameFlowContext context;
    [SerializeField] private bool playOnStart = true;

    private Coroutine _flowCoroutine;
    private IInGameFlowState _currentState;
    private bool _isChangingState;

    private void Awake()
    {
        if (context == null)
        {
            context = GetComponent<InGameFlowContext>();
        }
    }

    private void Start()
    {
        if (!playOnStart)
        {
            return;
        }

        StartFlow();
    }

    public void StartFlow()
    {
        if (_flowCoroutine != null)
        {
            return;
        }

        _flowCoroutine = StartCoroutine(FlowCoroutine());
    }

    public void ChangeState(IInGameFlowState nextState)
    {
        if (_isChangingState)
        {
            Debug.LogWarning("[InGameFlowController] ChangeState skipped. Reason=State is changing.");
            return;
        }

        StartCoroutine(ChangeStateCoroutine(nextState));
    }

    private IEnumerator FlowCoroutine()
    {
        yield return ChangeStateCoroutine(new GameStartFlowState());
        yield return ChangeStateCoroutine(new WaveIntroFlowState());
        yield return ChangeStateCoroutine(new PlayingFlowState());
        yield return ChangeStateCoroutine(new WaveEndFlowState());
        yield return ChangeStateCoroutine(new GameEndFlowState());

        _flowCoroutine = null;
    }

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
