using Context;
using UnityEngine;

// StageModelシーンの外部公開Context
public sealed class StageModelContext : ContextBase
{
    [SerializeField] private StageEndJudge stageEndJudge;
    [SerializeField] private Transform waveEndArea;
    [SerializeField] private InGamePresentationEventRegistry presentationEventRegistry;
    [SerializeField] private StageModelModeSystem stageModelModeSystem = new StageModelModeSystem();

    private void Start()
    {
        ApplySelectedGameMode();
    }

    // 現在選択中のゲームモードをStageModelへ反映
    public bool ApplySelectedGameMode()
    {
        if (stageModelModeSystem == null)
        {
            stageModelModeSystem = new StageModelModeSystem();
        }

        return stageModelModeSystem.Apply(gameObject.scene);
    }

    // Wave演出用の一時モデル切り替え
    public bool BeginThreePlayerPresentation()
    {
        return stageModelModeSystem != null && stageModelModeSystem.BeginThreePlayerPresentation(gameObject.scene);
    }

    public void EndThreePlayerPresentation()
    {
        stageModelModeSystem?.EndThreePlayerPresentation();
    }

    private void OnDisable()
    {
        EndThreePlayerPresentation();
    }

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
