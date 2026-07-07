using System.Collections;
using UnityEngine;

// インゲーム全体の進行Stateを順番に実行する制御
public sealed class InGameFlowController : MonoBehaviour
{
    [SerializeField] private InGameFlowContext context;
    [SerializeField] private bool playOnStart = true;

    private Coroutine _flowCoroutine;
    private IInGameFlowState _currentState;
    private bool _isChangingState;

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

    // 標準のインゲーム進行順
    private IEnumerator FlowCoroutine()
    {
        yield return ChangeStateCoroutine(new GameStartFlowState());
        yield return ChangeStateCoroutine(new WaveIntroFlowState());
        yield return ChangeStateCoroutine(new PlayingFlowState());
        yield return ChangeStateCoroutine(new WaveEndFlowState());
        yield return ChangeStateCoroutine(new GameEndFlowState());

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
