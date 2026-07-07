using UnityEngine;

// インゲーム進行Stateから参照する外部要素の保持
public sealed class InGameFlowContext : MonoBehaviour
{
    [SerializeField] private ForwardProgressMover forwardProgressMover;
    [SerializeField] private StageEndJudge stageEndJudge;

    // 前進制御
    public ForwardProgressMover ForwardProgressMover => forwardProgressMover;

    // ステージ終了判定
    public StageEndJudge StageEndJudge => stageEndJudge;
}
