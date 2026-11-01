using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.Timeline;

// 既存の開始Actionとアニメーションクリップをエンドレス用に組み合わせる。共通Timelineは編集しない。
[CreateAssetMenu(fileName = "EndlessRoundPresentation", menuName = "Game/InGame/Presentation/Endless Round")]
public sealed class InGameEndlessRoundPresentationAction : InGamePresentationAction
{
    [SerializeField] private InGamePresentationAction initialEntrance;
    [SerializeField] private AnimationClip npcClimb;
    [SerializeField] private TimelineAsset npcEntranceSource;
    [SerializeField] private AnimationClip[] finishPoses;
    private Action cancelPlayback;
    private EndlessEntranceCutIn entranceCutIn;

    public void CancelPlayback()
    {
        entranceCutIn?.Dispose();
        entranceCutIn = null;
        Action cancel = cancelPlayback;
        cancelPlayback = null;
        cancel?.Invoke();
    }

    public void ValidateConfiguration()
    {
        if (initialEntrance == null || npcClimb == null || npcClimb.length <= 0f ||
            finishPoses == null || finishPoses.Length == 0 ||
            Array.Exists(finishPoses, clip => clip == null || clip.length <= 0f))
        {
            throw new InvalidOperationException("[Endless] Entrance or finish animation configuration is missing.");
        }
        ResolveEntranceSection(out _, out _, out _);
    }

    private void ResolveEntranceSection(out TimelineClip climb, out TimelineClip movement, out TimelineClip pose)
    {
        climb = null;
        movement = null;
        pose = null;
        if (npcEntranceSource != null)
        {
            foreach (TrackAsset track in npcEntranceSource.GetOutputTracks())
            {
                if (track is not AnimationTrack) { continue; }
                foreach (TimelineClip candidate in track.GetClips())
                {
                    if (candidate.asset is AnimationPlayableAsset animation && animation.clip == npcClimb)
                    {
                        climb = candidate;
                        // 次に重なるクリップは、後続の歩行・飛ばしより前にある元の登場ポーズ（yaa）。
                        // カウントダウン前に、このポーズを最後まで再生する。
                        foreach (TimelineClip next in track.GetClips())
                        {
                            if (next != candidate && next.start >= candidate.start && next.start < candidate.end &&
                                next.asset is AnimationPlayableAsset nextAnimation && nextAnimation.clip != null &&
                                (pose == null || next.start < pose.start))
                            {
                                pose = next;
                            }
                        }
                        foreach (TrackAsset child in track.GetChildTracks())
                        foreach (TimelineClip motion in child.GetClips())
                        {
                            if (motion.asset is AnimationPlayableAsset && motion.start < candidate.end &&
                                motion.end <= candidate.end + 0.001d)
                            {
                                movement = motion;
                                break;
                            }
                        }
                        break;
                    }
                }
                if (climb != null) { break; }
            }
        }
        if (climb == null || movement == null || pose == null || pose.timeScale <= 0d || climb.timeScale <= 0d ||
            climb.clipIn < 0d || climb.clipIn >= npcClimb.length)
        {
            throw new InvalidOperationException("[Endless] The source cutscene's climb/movement section is missing or invalid. " +
                $"Source={npcEntranceSource}, Climb={climb != null}, Movement={movement != null}, Pose={pose != null}, ClipLength={npcClimb.length}");
        }
    }

    public override IEnumerator Play(InGameFlowContext context)
    {
        ValidateConfiguration();
        yield return initialEntrance.Play(context);
    }

    public IEnumerator PlayFinish(Player.PlayerPoseContoroller player, IEnumerator afterPose = null)
    {
        AnimationClip clip = finishPoses[UnityEngine.Random.Range(0, finishPoses.Length)];
        yield return PlayClip(player.PoseAnimator, clip, afterPose: afterPose);
    }

    public IEnumerator PlayIncomingCutIn(InGameFlowContext context, global::Enemy.Enemy enemy, Vector3 destination)
    {
        ResolveEntranceSection(out TimelineClip climb, out TimelineClip movement, out TimelineClip pose);
        using (var cutIn = new EndlessEntranceCutIn(context, enemy, destination, npcEntranceSource, climb, movement, pose))
        {
            entranceCutIn = cutIn;
            try { yield return cutIn.Play(); }
            finally { if (entranceCutIn == cutIn) entranceCutIn = null; }
        }
    }

    public IEnumerator PlayIncoming(global::Enemy.Enemy enemy, Vector3 destination)
    {
        ResolveEntranceSection(out TimelineClip climb, out TimelineClip movement, out TimelineClip pose);
        AnimationClip motion = ((AnimationPlayableAsset)movement.asset).clip;
        var sample = new GameObject("Endless entrance motion sample") { hideFlags = HideFlags.HideAndDontSave };
        try
        {
            // 記録済みの登り経路を着地点からの相対位置として再利用し、選択したSlotに合わせる。
            // 元のTimeline自体は再生しないため、参加NPCに飛ばし・カメラ・音声・Signalの
            // 処理が実行されることはない。
            float finalMotionTime = (float)(movement.clipIn + movement.duration * movement.timeScale);
            motion.SampleAnimation(sample, finalMotionTime);
            Vector3 landingPosition = sample.transform.localPosition;
            Quaternion landingRotation = sample.transform.localRotation;
            Action<float> placeActor = progress =>
            {
                if (enemy == null) { return; }
                double sourceTime = climb.start + climb.duration * progress;
                float motionTime = (float)(movement.clipIn +
                    Math.Max(0d, Math.Min(movement.duration, sourceTime - movement.start)) * movement.timeScale);
                motion.SampleAnimation(sample, motionTime);
                enemy.transform.SetPositionAndRotation(destination + sample.transform.localPosition - landingPosition,
                    sample.transform.localRotation * Quaternion.Inverse(landingRotation));
            };
            placeActor(0f);
            enemy.gameObject.SetActive(true);
            yield return PlayClip(enemy.GetComponentInChildren<Animator>(), npcClimb, placeActor,
                climb.clipIn, climb.clipIn + climb.duration * climb.timeScale, true, pose, climb);
            if (enemy == null) { throw new InvalidOperationException("[Endless] Incoming NPC was destroyed during its entrance."); }
            enemy.transform.SetPositionAndRotation(destination, Quaternion.identity);
        }
        finally { if (sample != null) { Destroy(sample); } }
    }

    private IEnumerator PlayClip(Animator animator, AnimationClip clip, Action<float> onProgress = null,
        double startTime = 0d, double endTime = -1d, bool blendToEntrancePose = false,
        TimelineClip entrancePose = null, TimelineClip climbSection = null, IEnumerator afterPose = null)
    {
        if (animator == null || !animator.isActiveAndEnabled || animator.avatar == null)
        {
            throw new InvalidOperationException("[Endless] Presentation Animator or Avatar is unavailable.");
        }

        bool originalRootMotion = animator.applyRootMotion;
        AnimatorCullingMode originalCulling = animator.cullingMode;
        float originalSpeed = animator.speed;
        Vector3 localPosition = animator.transform.localPosition;
        Quaternion localRotation = animator.transform.localRotation;
        Vector3 localScale = animator.transform.localScale;
        int entrancePoseState = Animator.StringToHash("Base Layer.StartPose.START" +
            (char)('A' + animator.GetInteger("StartPoseIndex")));
        // 参加NPC専用のコピーからクリップイベントを除く。元のクリップとフィニッシュ用クリップは維持する。
        AnimationClip playbackClip = blendToEntrancePose ? Instantiate(clip) : clip;
        if (blendToEntrancePose) { playbackClip.events = Array.Empty<AnimationEvent>(); }
        AnimationClip poseClip = entrancePose != null
            ? Instantiate(((AnimationPlayableAsset)entrancePose.asset).clip) : null;
        if (poseClip != null) { poseClip.events = Array.Empty<AnimationEvent>(); }
        // 元のTimelineは、記録された経路が終わるまでClimbの最終フレームを保持する。
        double clipEnd = endTime < 0d ? clip.length : endTime;
        double poseStart = entrancePose != null
            ? startTime + (entrancePose.start - climbSection.start) * climbSection.timeScale : clipEnd;
        double sequenceEnd = entrancePose != null
            ? startTime + (entrancePose.end - climbSection.start) * climbSection.timeScale : clipEnd;
        PlayableGraph graph = PlayableGraph.Create("Endless " + clip.name);
        bool released = false;
        Action release = () =>
        {
            if (released) { return; }
            released = true;
            if (graph.IsValid()) { graph.Destroy(); }
            if (blendToEntrancePose && playbackClip != null) { Destroy(playbackClip); }
            if (poseClip != null) { Destroy(poseClip); }
            if (animator == null) { return; }
            animator.applyRootMotion = originalRootMotion;
            animator.cullingMode = originalCulling;
            animator.speed = originalSpeed;
            // ゲーム用ルートをSlotへ移動するため、モデルのローカルなアニメーションオフセットを復元する。
            if (onProgress == null || animator.GetComponent<global::Enemy.Enemy>() == null)
            {
                animator.transform.localPosition = localPosition;
                animator.transform.localRotation = localRotation;
                animator.transform.localScale = localScale;
            }
            if (animator.isActiveAndEnabled)
            {
                if (blendToEntrancePose)
                {
                    animator.Play(entrancePoseState, 0, 0f);
                    animator.Update(0f);
                }
                else { animator.GetComponent<OnStartPose>()?.SetTriggerIdle(); }
            }
        };
        CancelPlayback();
        cancelPlayback = release;
        try
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.speed = 1f;
            AnimationClipPlayable playable = AnimationClipPlayable.Create(graph, playbackClip);
            playable.SetDuration(clipEnd);
            playable.SetSpeed(0d);
            playable.SetApplyFootIK(false);
            Playable sequence = playable;
            AnimationClipPlayable posePlayable = default;
            AnimationMixerPlayable entranceMixer = default;
            if (poseClip != null)
            {
                posePlayable = AnimationClipPlayable.Create(graph, poseClip);
                posePlayable.SetSpeed(0d);
                posePlayable.SetApplyFootIK(false);
                entranceMixer = AnimationMixerPlayable.Create(graph, 2);
                graph.Connect(playable, 0, entranceMixer, 0);
                graph.Connect(posePlayable, 0, entranceMixer, 1);
                sequence = entranceMixer;
            }
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Actor", animator);
            output.SetSourcePlayable(sequence);
            // クリップ時刻はこの処理で制限し、出力の評価はUnityのアニメーション処理に任せる。
            // コルーチン内でグラフを手動評価すると、描画前にキャラクターの通常の
            // AnimatorControllerで結果が上書きされてしまう。
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            graph.Play();

            float elapsed = 0f;
            double time = startTime;
            while (true)
            {
                if (animator == null || !animator.isActiveAndEnabled)
                {
                    throw new InvalidOperationException("[Endless] Presentation actor became unavailable.");
                }

                playable.SetTime(Math.Min(Math.Min(time, clipEnd), playbackClip.length));
                if (poseClip != null)
                {
                    double poseTime = entrancePose.clipIn + Math.Max(0d, time - poseStart) /
                        climbSection.timeScale * entrancePose.timeScale;
                    posePlayable.SetTime(Math.Min(poseTime, poseClip.length));
                    float weight = time < poseStart ? 0f : clipEnd <= poseStart ? 1f :
                        Mathf.Clamp01((float)((time - poseStart) / (clipEnd - poseStart)));
                    entranceMixer.SetInputWeight(0, 1f - weight);
                    entranceMixer.SetInputWeight(1, weight);
                }
                graph.Evaluate(0f);
                onProgress?.Invoke(Mathf.Clamp01((float)((time - startTime) / (clipEnd - startTime))));
                if (time >= sequenceEnd) { break; }
                elapsed += Time.deltaTime;
                if (elapsed > Mathf.Max(30f, clip.length * 3f))
                {
                    throw new InvalidOperationException("[Endless] Animation playback stopped advancing: " + clip.name);
                }
                yield return null;
                time = Math.Min(sequenceEnd, time + Time.deltaTime);
            }
            // 次の遷移で画面が覆われるまで、完了したフィニッシュポーズを接続したまま保持する。
            // 中断時も同じfinallyを通り、解放とIdleへの復元を行う。
            if (afterPose != null) { yield return afterPose; }
            if (blendToEntrancePose)
            {
                // 初期状態が非アクティブの参加NPCは、表示後にOnStartPose.Startが実行される。
                // PrefabのStart前の値を保存せず、この時点で選択されたポーズを読み取る。
                entrancePoseState = Animator.StringToHash("Base Layer.StartPose.START" +
                    (char)('A' + animator.GetInteger("StartPoseIndex")));
                var controller = AnimatorControllerPlayable.Create(graph, animator.runtimeAnimatorController);
                controller.SetInteger("StartPoseIndex", animator.GetInteger("StartPoseIndex"));
                controller.Play(entrancePoseState, 0, 0f);
                controller.SetSpeed(0d);
                playable.SetSpeed(0d);
                var mixer = AnimationMixerPlayable.Create(graph, 2);
                graph.Connect(sequence, 0, mixer, 0);
                graph.Connect(controller, 0, mixer, 1);
                output.SetSourcePlayable(mixer);
                // NPCの参加を確定する前に、登りの完了状態から既存の登場ポーズへブレンドする。
                // カウントダウンでも同じ選択済みポーズを使い、再度の不自然な切り替わりを防ぐ。
                float blend = 0f;
                while (blend < 1f)
                {
                    if (animator == null || !animator.isActiveAndEnabled)
                    {
                        throw new InvalidOperationException("[Endless] Incoming actor became unavailable during its landing.");
                    }
                    blend = Mathf.Min(1f, blend + Time.deltaTime / 0.3f);
                    mixer.SetInputWeight(0, 1f - blend);
                    mixer.SetInputWeight(1, blend);
                    playable.SetTime(Math.Min(clipEnd, playbackClip.length));
                    graph.Evaluate(0f);
                    onProgress?.Invoke(1f);
                    yield return null;
                }
            }
        }
        finally
        {
            release();
            if (cancelPlayback == release) { cancelPlayback = null; }
        }
    }
}
