using System;
using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Enemy;
using UnityEngine;
using Object = UnityEngine.Object;

// 試合進行を独立して管理する。グランプリは従来のStateの順序を使用する。
public sealed class EndlessRoundFlow : IDisposable
{
    private readonly StageData stage;
    private readonly InGameFlowContext context;
    private readonly Action<int, WaveData> setWave;
    private readonly InGameEndlessRoundPresentationAction presentation;
    private bool disposed;
    private bool ownsLock;
    private bool ownsRoundTransition;
    private IntroCoutDown countdown;
    private IntroSmoke introSmoke;
    private Player.PlayerPoseContoroller player;
    private Quaternion cameraRotation;
    private Quaternion playerRotation;
    private Vector3 playerScale;
    private Action stageEnded;
    private readonly Stack<IEnumerator> routines = new();
    private readonly HashSet<OnStartPose> countdownActors = new();

    public string Phase { get; private set; } = "EndlessInitializing";

    public EndlessRoundFlow(StageData stage, InGameFlowContext context, Action<int, WaveData> setWave)
    {
        this.stage = stage;
        this.context = context;
        this.setWave = setWave;
        presentation = stage.GameStartAction as InGameEndlessRoundPresentationAction;
    }

    // 入れ子のActionやアニメーションの失敗は、敗北ではなく設定・実行時の異常として扱う。
    public IEnumerator Run()
    {
        routines.Push(RunRounds());
        Exception failure = null;
        try
        {
            while (!disposed && routines.Count > 0)
            {
                IEnumerator current = routines.Peek();
                bool next = false;
                try { next = current.MoveNext(); }
                catch (Exception error) { failure = error; }
                if (disposed) { break; }
                if (failure != null) { break; }
                if (!next)
                {
                    routines.Pop();
                    (current as IDisposable)?.Dispose();
                }
                else if (current.Current is IEnumerator nested) { routines.Push(nested); }
                else { yield return current.Current; }
            }
            if (failure != null && !disposed)
            {
                ClearRoundTransition();
                EnterPresentation();
                Phase = "EndlessFaulted";
                Debug.LogError("[Endless] Progression stopped safely; this is not a defeat. " + failure);
                while (!disposed) { yield return null; }
            }
        }
        finally
        {
            try
            {
                DisposeRoutines();
            }
            finally { Dispose(); }
        }
    }

    private IEnumerator RunRounds()
    {
        EnterPresentation();
        Require(context != null && context.EnemyLineup != null && context.ScoreSource != null &&
            context.StageEndJudge != null && context.ForwardProgressMover != null && context.WaveGauge != null,
            "Required gameplay services are missing.");
        Require(presentation != null, "Endless round presentation is not assigned.");
        presentation.ValidateConfiguration();
        Require(stage.InitialEnemyCount == EnemyMaxCount.One && stage.MaxEnemyCount == EnemyMaxCount.Four,
            "Endless requires one initial NPC and four available Slots.");
        GameModeManager mode = GameModeManager.Instance;
        Require(mode != null && mode.IsInitialized && mode.CurrentGameMode == GameMode.FivePlayer,
            "Five-player mode has not initialized.");
        Require(context.ResetPositionAction is InGameSetActorPositionsPresentationAction,
            "Slot placement Action is missing.");
        player = Object.FindFirstObjectByType<Player.PlayerPoseContoroller>();
        countdown = Object.FindFirstObjectByType<IntroCoutDown>();
        introSmoke = Object.FindFirstObjectByType<IntroSmoke>();
        Require(player != null && countdown != null && context.ForwardProgressMover.CameraTransform != null,
            "Player, countdown or gameplay camera is missing.");
        playerRotation = player.MoveTargetTransform.rotation;
        playerScale = player.MoveTargetTransform.localScale;
        cameraRotation = context.ForwardProgressMover.CameraTransform.rotation;
        context.WaveGauge.DeferStageEndTransition = true;

        int index = 0;
        WaveData wave = stage.GetWaveOrNull(index);
        Require(wave != null && stage.HasWave(1), "Initial Wave or endless Wave pool is missing.");
        setWave(index, wave);
        Phase = "EndlessInitialEntrance";
        context.ResetCameraPosition(stage);
        // 演出と入力のロックを維持したまま、登場時からHUDを表示する。
        context.WaveGauge.EndWavePresentation();
        context.WaveGauge.HideWaveFinalForGameEnd();
        // 有効化後の最初の粒子発生を待たず、開始時点で粒子が見えるようにする。
        introSmoke?.ClearSmoke();
        introSmoke?.BeginEndlessEntrance();
        yield return new GameStartFlowState(stage).Execute(context);
        Require(context.EnemyLineup.CurrentEnemyCount == 1 && context.EnemyLineup.MaxEnemyCount == 4,
            "Initial NPC generation failed.");
        yield return PrepareRound(index, wave);

        while (!disposed)
        {
            bool ended = false;
            stageEnded = () =>
            {
                if (ended) { return; }
                ended = true;
                EnterPresentation();
            };
            context.StageEndJudge.OnStageEnd += stageEnded;
            try
            {
                context.StageEndJudge.ResetJudge();
                context.StageEndJudge.IsRoundSuspended = false;
                Phase = "PlayingFlowState";
                ExitPresentation();
                context.ForwardProgressMover.ResumeMove();
                while (!ended && !disposed)
                {
                    Require(context.StageEndJudge != null, "Stage end judge was destroyed.");
                    yield return null;
                }
            }
            finally
            {
                if (context != null && context.StageEndJudge != null)
                {
                    context.StageEndJudge.OnStageEnd -= stageEnded;
                }
                stageEnded = null;
            }
            if (disposed) { yield break; }

            // リセットや生成を行う前に、同じフレームで演出ロックをかけて得点を確定する。
            int score = context.CurrentPlayerScore;
            ResetActorStates();
            if (score < stage.GetTargetScore(index))
            {
                Phase = "GameEndFlowState";
                yield return TransitionManager.PlayDumbbellFadeOut().ToCoroutine();
                yield return new GameEndFlowState(stage).Execute(context);
                yield break;
            }

            GameResultManager.Instance?.RecordWaveCleared(index + 1);
            Require(stage.HasWave(index + 1), "Endless Wave pool is no longer available.");
            WaveData nextWave = stage.GetWaveOrNull(index + 1);
            Require(nextWave != null && nextWave.HasReplacementEnemy, "Next Wave has no replacement EnemyData.");
            EnemyLineupManager lineup = context.EnemyLineup;
            int previousCount = lineup.CurrentEnemyCount;
            // 前試合の個別スコアが残っている間に、空きSlotまたは最低得点のSlotを選ぶ。
            global::Enemy.Enemy incoming = lineup.BeginWaveEnemyChange(nextWave.ReplacementEnemyData,
                nextWave.ReplacementEnemyPosition, nextWave.ReplacementEnemyRotation);
            Require(incoming != null && lineup.HasPendingReplacement, "NPC generation failed.");
            incoming.SetRoundParticipation(false);
            incoming.gameObject.SetActive(false);

            Phase = "EndlessFinishPose";
            context.WaveGauge.ShowEndlessFinishPose();
            context.AudienceRequest?.ResetRequests();
            // フィニッシュアニメーションの開始前に、既存の案内を描画する。
            yield return null;
            yield return presentation.PlayFinish(player, CoverForIncomingCutIn());

            Phase = "EndlessNpcEntrance";
            context.WaveGauge.BeginWavePresentation();
            context.WaveGauge.HideWaveFinalForGameEnd();
            global::Enemy.Enemy outgoing = lineup.PendingOldEnemy;
            if (outgoing != null)
            {
                outgoing.SetRoundParticipation(false);
                outgoing.gameObject.SetActive(false);
            }
            var positions = (InGameSetActorPositionsPresentationAction)context.ResetPositionAction;
            Vector3 destination = positions.GetFivePlayerEnemyPosition(lineup.PendingReplacementSlot);
            // 蒸気は初回登場時だけ使用する。以降のNPCは登りとポーズを再生し、
            // 蒸気を再発生させずに、そのままカウントダウンへ進む。
            ClearIntroSmoke();
            try { yield return presentation.PlayIncomingCutIn(context, incoming, destination); }
            finally
            {
                ClearIntroSmoke();
                ClearRoundTransition();
            }
            Require(lineup.CommitPendingReplacement() == incoming, "NPC replacement could not be committed.");
            incoming.SetRoundParticipation(true);
            Require(lineup.CurrentEnemyCount == Mathf.Min(previousCount + 1, 4), "Unexpected NPC count after commit.");

            index++;
            wave = nextWave;
            yield return PrepareRound(index, wave);
        }
    }

    private IEnumerator CoverForIncomingCutIn()
    {
        Phase = "EndlessCutInFade";
        context.WaveGauge.HideWaveFinalForGameEnd();
        ownsRoundTransition = true;
        // グランプリの落下ダンベルを再利用し、実際に画面を覆い終わるまで待つ。
        yield return TransitionManager.PlayDumbbellFadeOut().ToCoroutine();
    }

    private void ClearRoundTransition()
    {
        if (!ownsRoundTransition) { return; }
        ownsRoundTransition = false;
        TransitionManager.ResetImmediate();
    }

    private IEnumerator PrepareRound(int index, WaveData wave)
    {
        Phase = "EndlessRoundReset";
        setWave(index, wave);
        ResetActorStates();
        context.AudienceRequest?.ResetRequests();
        // 交代するSlotは、前試合の得点をもとに既に選択・確定されている。
        context.ScoreSource.ResetAllScores();
        context.EnemyLineup.SetAllEnemyAnimationSpeed(stage.GetEnemyAnimationSpeed(index));
        context.RestoreWavePresentationStage();
        yield return context.ResetPositionAction.Play(context);
        player.MoveTargetTransform.rotation = playerRotation;
        player.MoveTargetTransform.localScale = playerScale;
        context.ResetCameraPosition(stage);
        context.ForwardProgressMover.CameraTransform.rotation = cameraRotation;
        context.ForwardProgressMover.ResetStageEndJudgePosition();
        context.WaveGauge.ResetWaveGauge(false);
        HoldCountdownActors();
        Phase = "EndlessCountdown";
        Sound.Manager.SoundManager.Stop((int)Sound.Manager.SoundManager.SceneSoundTag.InGameSound,
            (int)Sound.Manager.SoundManager.InGameSoundTag.InGameBGM);
        countdown.StartCountDown();
        float elapsed = 0f;
        while (countdown != null && !countdown.IsFinished)
        {
            Require(countdown.isActiveAndEnabled && countdown.IsRunning, "Countdown stopped before completion.");
            elapsed += Time.deltaTime;
            Require(elapsed < 30f, "Countdown did not finish.");
            yield return null;
        }
        Require(countdown != null, "Countdown was destroyed.");
        ReleaseCountdownActors();
    }

    private void HoldCountdownActors()
    {
        ReleaseCountdownActors();
        HoldCountdownActor(player.MoveTargetTransform);
        for (int slot = 1; slot <= context.EnemyLineup.MaxEnemyCount; slot++)
        {
            global::Enemy.Enemy enemy = context.EnemyLineup.GetEnemy((EnemySlotId)slot);
            if (enemy != null) { HoldCountdownActor(enemy.transform); }
        }
    }

    private void HoldCountdownActor(Transform actor)
    {
        foreach (OnStartPose animation in actor.GetComponentsInChildren<OnStartPose>())
        {
            countdownActors.Add(animation);
            animation.SetCountdownHold(true);
        }
    }

    private void ReleaseCountdownActors()
    {
        foreach (OnStartPose animation in countdownActors)
        {
            if (animation != null) { animation.SetCountdownHold(false); }
        }
        countdownActors.Clear();
    }

    private void ResetActorStates()
    {
        player?.ResetForRound();
        EnemyLineupManager lineup = context.EnemyLineup;
        for (int slot = 1; slot <= lineup.MaxEnemyCount; slot++)
        {
            lineup.GetEnemy((EnemySlotId)slot)?.ResetForRound();
        }
        Object.FindFirstObjectByType<EnemyPoseController>()?.ResetForRound();
        Object.FindFirstObjectByType<FeverGaugeController>()?.ResetForRound();
    }

    private void EnterPresentation()
    {
        if (!ownsLock) { InGameFlowLock.BeginPresentation(); ownsLock = true; }
        if (context != null)
        {
            context.ForwardProgressMover?.StopMove();
            if (context.StageEndJudge != null) { context.StageEndJudge.IsRoundSuspended = true; }
            context.AudienceRequest?.ResetRequests();
        }
    }

    private void ExitPresentation()
    {
        if (!ownsLock) { return; }
        ownsLock = false;
        InGameFlowLock.EndPresentation();
    }

    public void Dispose()
    {
        if (disposed) { return; }
        disposed = true;
        // UnityのStopCoroutineでは、入れ子のイテレーターが確実に破棄されるとは限らない。
        // Actionを明示的に巻き戻し、GameStartFlowStateとGameEndFlowState内のロックも解放する。
        try { DisposeRoutines(); }
        finally { RestoreAfterStop(); }
    }

    private void DisposeRoutines()
    {
        if (routines.Count == 0) { return; }
        try { (routines.Pop() as IDisposable)?.Dispose(); }
        finally { DisposeRoutines(); }
    }

    private void RestoreAfterStop()
    {
        ClearRoundTransition();
        ReleaseCountdownActors();
        ClearIntroSmoke();
        if (presentation != null) { presentation.CancelPlayback(); }
        if (countdown != null && countdown.IsRunning) { countdown.CancelCountDown(); }
        if (context != null)
        {
            if (context.StageEndJudge != null)
            {
                if (stageEnded != null) { context.StageEndJudge.OnStageEnd -= stageEnded; }
                context.StageEndJudge.IsRoundSuspended = false;
            }
            EnemyLineupManager lineup = context.EnemyLineup;
            if (lineup != null && lineup.HasPendingReplacement)
            {
                if (lineup.PendingOldEnemy != null)
                {
                    lineup.PendingOldEnemy.gameObject.SetActive(true);
                    lineup.PendingOldEnemy.SetRoundParticipation(true);
                }
                lineup.CancelPendingReplacement();
            }
            if (context.WaveGauge != null)
            {
                context.WaveGauge.HideWaveFinalForGameEnd();
                context.WaveGauge.DeferStageEndTransition = false;
            }
            context.RestoreWavePresentationStage();
        }
        ExitPresentation();
    }

    private void ClearIntroSmoke()
    {
        // この進行処理のOnDisableが呼ばれる前に、StageModelが既にアンロードされている場合がある。
        if (introSmoke != null && introSmoke.gameObject.scene.isLoaded) { introSmoke.ClearSmoke(); }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) { throw new InvalidOperationException("[Endless] " + message); }
    }
}
