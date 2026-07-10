using RuntimeDebug;
using UnityEngine;

public sealed class StateTraker : MonoBehaviour
{
    private const string DebugWindowName = "InGame State";

    [SerializeField] private InGameFlowController flowController;
    [SerializeField] private InGameFlowContext flowContext;

    private void Awake()
    {
        ResolveReferences();
    }

    private void Start()
    {
        ResolveReferences();
        CreateDebugMenu();
    }

    private void OnDestroy()
    {
        RuntimeDebug.DebugMenu.DestroyWindow(DebugWindowName);
    }

    private void ResolveReferences()
    {
        if (flowController == null)
        {
            flowController = FindFirstObjectByType<InGameFlowController>();
        }

        if (flowContext == null)
        {
            flowContext = FindFirstObjectByType<InGameFlowContext>();
        }
    }

    private void CreateDebugMenu()
    {
        DebugWindow window = RuntimeDebug.DebugMenu.CreateWindow(DebugWindowName);
        window.CreateMessage("State", GetCurrentStateName);
        window.CreateMessage("Wave Index", GetCurrentWaveIndex);
        window.CreateMessage("Player Score", GetCurrentPlayerScore);
        window.CreateMessage("Target Score", GetCurrentTargetScore);
        window.CreateMessage("Flow Running", GetIsFlowRunning);
        window.CreateMessage("Changing State", GetIsChangingState);
        window.CreateMessage("Presentation Lock", GetPresentationLock);
    }

    private string GetCurrentStateName()
    {
        ResolveReferences();
        return flowController != null ? flowController.CurrentStateName : "None";
    }

    private string GetCurrentWaveIndex()
    {
        ResolveReferences();
        return flowController != null ? flowController.CurrentWaveIndex.ToString() : "-";
    }

    private string GetCurrentPlayerScore()
    {
        ResolveReferences();
        return flowContext != null ? flowContext.CurrentPlayerScore.ToString() : "-";
    }

    private string GetCurrentTargetScore()
    {
        ResolveReferences();
        return flowController != null ? flowController.CurrentTargetScore.ToString() : "-";
    }

    private string GetIsFlowRunning()
    {
        ResolveReferences();
        return flowController != null ? flowController.IsFlowRunning.ToString() : "False";
    }

    private string GetIsChangingState()
    {
        ResolveReferences();
        return flowController != null ? flowController.IsChangingState.ToString() : "False";
    }

    private string GetPresentationLock()
    {
        return InGameFlowLock.IsPresentationPlaying.ToString();
    }
}
