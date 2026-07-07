using UnityEngine;

public sealed class InGameFlowContext : MonoBehaviour
{
    [SerializeField] private ForwardProgressMover forwardProgressMover;
    [SerializeField] private StageEndJudge stageEndJudge;

    public ForwardProgressMover ForwardProgressMover => forwardProgressMover;
    public StageEndJudge StageEndJudge => stageEndJudge;
}
