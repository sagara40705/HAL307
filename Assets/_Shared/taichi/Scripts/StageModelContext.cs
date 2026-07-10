using Context;
using UnityEngine;

// StageModelシーンの外部公開Context
public sealed class StageModelContext : ContextBase
{
    [SerializeField] private StageEndJudge stageEndJudge;
    [SerializeField] private Transform waveEndArea;
    [SerializeField] private InGamePresentationEventRegistry presentationEventRegistry;


    // ステージ終了判定のTransform
    // ステージ終了判定
    public StageEndJudge StageEndJudge => stageEndJudge;

    public Transform StageEndJudgeTransform => stageEndJudge != null ? stageEndJudge.transform : null;

    // Wave終了エリアのTransform
    public Transform WaveEndAreaTransform => waveEndArea;

    // 演出イベントRegistry
    public InGamePresentationEventRegistry PresentationEventRegistry => presentationEventRegistry;

    // ステージ終了判定の位置
    public Vector3 StageEndJudgePosition => StageEndJudgeTransform != null ? StageEndJudgeTransform.position : Vector3.zero;

    // Wave終了エリアの位置
    public Vector3 WaveEndAreaPosition => waveEndArea != null ? waveEndArea.position : Vector3.zero;
}
