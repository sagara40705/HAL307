using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using Object = UnityEngine.Object;

// 既存カットシーンを実行時だけ登場用に編集する。共通のScene・Timelineアセットは変更しない。
public sealed class EndlessEntranceCutIn : IDisposable
{
    private const string SceneName = "CutScene";
    private readonly InGameFlowContext context;
    private readonly global::Enemy.Enemy incoming;
    private readonly Vector3 destination;
    private readonly TimelineAsset source;
    private readonly TimelineClip climb;
    private readonly TimelineClip movement;
    private readonly TimelineClip pose;
    private readonly List<Object> runtimeObjects = new();
    private Scene scene;
    private PlayableDirector director;
    private StageModelContext stage;
    private bool ownsStageOverride;
    private Camera gameplayCamera;
    private bool cameraWasEnabled;
    private AudioListener gameplayListener;
    private bool listenerWasEnabled;
    private Transform originalParent;
    private Scene originalScene;
    private Vector3 originalScale;
    private Animator incomingAnimator;
    private bool originalRootMotion;
    private AnimatorCullingMode originalCulling;
    private bool actorMoved;
    private bool ownsScene;
    private bool loading;
    private bool completed;
    private bool disposed;

    public EndlessEntranceCutIn(InGameFlowContext context, global::Enemy.Enemy incoming, Vector3 destination,
        TimelineAsset source, TimelineClip climb, TimelineClip movement, TimelineClip pose)
    {
        this.context = context;
        this.incoming = incoming;
        this.destination = destination;
        this.source = source;
        this.climb = climb;
        this.movement = movement;
        this.pose = pose;
    }

    public IEnumerator Play()
    {
        try
        {
            if (SceneManager.GetSceneByName(SceneName).isLoaded || !Application.CanStreamedLevelBeLoaded(SceneName))
                throw new InvalidOperationException("[Endless] Entrance cut-in scene is unavailable or already in use.");
            ownsScene = true;
            loading = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
            if (SceneLoader.Instance != null && SceneLoader.Instance.IsInitialized)
                yield return SceneLoader.Instance.LoadAdditiveAsync(SceneName);
            else
                yield return SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Additive);
            loading = false;
            scene = SceneManager.GetSceneByName(SceneName);
            if (disposed) { yield break; }
            if (!scene.isLoaded) throw new InvalidOperationException("[Endless] Entrance cut-in scene did not load.");

            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (PlayableDirector candidate in root.GetComponentsInChildren<PlayableDirector>(true))
                if (candidate.playableAsset == source) { director = candidate; break; }
            if (director == null) throw new InvalidOperationException("[Endless] Source cut-in director was not found.");
            var controller = director.GetComponent<CutsceneTimelineController>();
            if (controller == null) throw new InvalidOperationException("[Endless] Source cut-in controller was not found.");
            controller.enabled = false;
            foreach (GameObject root in scene.GetRootGameObjects())
            foreach (CutsceneDebuger debug in root.GetComponentsInChildren<CutsceneDebuger>(true)) debug.enabled = false;
            director.Stop();

            var timeline = ScriptableObject.CreateInstance<TimelineAsset>();
            timeline.name = "Endless entrance cut-in (runtime)";
            runtimeObjects.Add(timeline);
            timeline.durationMode = TimelineAsset.DurationMode.FixedLength;
            timeline.fixedDuration = pose.end;
            foreach (TrackAsset root in source.GetOutputTracks())
            {
                if (root is not AnimationTrack animation) { continue; }
                bool isIncoming = false;
                foreach (TimelineClip clip in animation.GetClips()) if (clip == climb) isIncoming = true;
                Object binding = director.GetGenericBinding(animation);
                if (isIncoming)
                {
                    Animator template = binding as Animator;
                    if (template == null) throw new InvalidOperationException("[Endless] Cut-in entrance actor binding is missing.");
                    MoveIncomingToCutIn(template.transform);
                    binding = incoming.GetComponentInChildren<Animator>(true);
                }
                bool isCamera = binding is Animator cameraAnimator && cameraAnimator.GetComponent<Camera>() != null;
                if (isCamera && animation.infiniteClip != null) PrepareCameraAnchor((Animator)binding, animation);
                AnimationTrack copy = CopyTrack(timeline, null, animation, isIncoming, false,
                    isCamera);
                director.SetGenericBinding(copy, binding);
            }

            stage = Object.FindFirstObjectByType<StageModelContext>();
            ownsStageOverride = stage != null && stage.BeginThreePlayerPresentation();
            gameplayCamera = context.ForwardProgressMover.CameraTransform.GetComponent<Camera>();
            if (gameplayCamera != null)
            {
                cameraWasEnabled = gameplayCamera.enabled;
                gameplayCamera.enabled = false;
                gameplayListener = gameplayCamera.GetComponent<AudioListener>();
                if (gameplayListener != null)
                {
                    listenerWasEnabled = gameplayListener.enabled;
                    gameplayListener.enabled = false;
                }
            }
            director.playableAsset = timeline;
            director.extrapolationMode = DirectorWrapMode.Hold;
            director.timeUpdateMode = DirectorUpdateMode.GameTime;
            controller.PlayFromStart();
            // ダンベルのフェードで画面が覆われている間に、カットインの最初のフレームを準備する。
            // 通常の表示完了通知を待ってから、登場アニメーションを進める。
            director.Pause();
            yield return TransitionManager.RevealIfCovered();
            director.Play();

            while (director != null && director.time < pose.end)
            {
                if (incoming == null || !incoming.gameObject.activeInHierarchy)
                    throw new InvalidOperationException("[Endless] Incoming cut-in actor became unavailable.");
                yield return null;
            }
            if (director == null) throw new InvalidOperationException("[Endless] Entrance cut-in was destroyed before completion.");
            completed = true;
        }
        finally { Dispose(); }

        // カットインを隠した後は待機せず、呼び出し元で同じフレーム内に参加者を確定し、
        // ゲーム用カメラとカウントダウンを初期化する。シーンの削除完了はローダー側で処理する。
    }

    private void PrepareCameraAnchor(Animator animator, AnimationTrack cameraTrack)
    {
        // この記録済みカメラにはAvatarやルートモーションのバインドがない。無限トラックの元の
        // オフセットを親に設定し、クリップ形式への変換時にUnityが破棄しないようにする。
        var anchor = new GameObject("Endless cut-in camera anchor");
        runtimeObjects.Add(anchor);
        anchor.transform.SetParent(animator.transform.parent, false);
        anchor.transform.localPosition = cameraTrack.infiniteClipOffsetPosition;
        anchor.transform.localRotation = cameraTrack.infiniteClipOffsetRotation;
        animator.transform.SetParent(anchor.transform, false);
        animator.transform.localPosition = Vector3.zero;
        animator.transform.localRotation = Quaternion.identity;
    }

    private void MoveIncomingToCutIn(Transform template)
    {
        originalParent = incoming.transform.parent;
        originalScene = incoming.gameObject.scene;
        originalScale = incoming.transform.localScale;
        incomingAnimator = incoming.GetComponentInChildren<Animator>(true);
        originalRootMotion = incomingAnimator.applyRootMotion;
        originalCulling = incomingAnimator.cullingMode;
        incomingAnimator.applyRootMotion = template.GetComponent<Animator>().applyRootMotion;
        incomingAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        actorMoved = true;
        incoming.transform.SetParent(null, true);
        SceneManager.MoveGameObjectToScene(incoming.gameObject, scene);
        incoming.transform.SetParent(template.parent, false);
        incoming.transform.localPosition = template.localPosition;
        incoming.transform.localRotation = template.localRotation;
        incoming.transform.localScale = template.localScale;
        template.gameObject.SetActive(false);
        incoming.gameObject.SetActive(true);
    }

    private AnimationTrack CopyTrack(TimelineAsset timeline, AnimationTrack parent, AnimationTrack sourceTrack,
        bool isIncoming, bool isChild, bool isCamera)
    {
        var track = timeline.CreateTrack<AnimationTrack>(parent, sourceTrack.name);
        runtimeObjects.Add(track);
        track.position = sourceTrack.position;
        track.rotation = sourceTrack.rotation;
        track.trackOffset = sourceTrack.trackOffset;
        track.matchTargetFields = sourceTrack.matchTargetFields;
        track.avatarMask = sourceTrack.avatarMask;
        track.applyAvatarMask = sourceTrack.applyAvatarMask;
        if (sourceTrack.infiniteClip != null)
        {
            TimelineClip clip = track.CreateClip(CloneAnimation(sourceTrack.infiniteClip));
            runtimeObjects.Add(clip.asset);
            clip.start = 0d;
            clip.duration = pose.end;
            var asset = (AnimationPlayableAsset)clip.asset;
            asset.position = isCamera ? Vector3.zero : sourceTrack.infiniteClipOffsetPosition;
            asset.rotation = isCamera ? Quaternion.identity : sourceTrack.infiniteClipOffsetRotation;
            asset.removeStartOffset = false;
            asset.loop = AnimationPlayableAsset.LoopMode.Off;
        }
        foreach (TimelineClip original in sourceTrack.GetClips())
        {
            // 参加NPCには登り・ポーズ全体・着地経路だけを使い、6.88秒からの歩行と7.01秒からの
            // 飛ばす経路はコピーしない。他のキャラクターも飛ばされる前に止める。
            if (isIncoming ? isChild ? original != movement : original != climb && original != pose
                : original.start >= climb.end) { continue; }
            if (original.asset is not AnimationPlayableAsset animation || animation.clip == null) { continue; }
            TimelineClip clip = track.CreateClip(CloneAnimation(animation.clip));
            runtimeObjects.Add(clip.asset);
            clip.start = original.start;
            clip.clipIn = original.clipIn;
            // 着地と見守る側のクリップは、それぞれの非ループPlayable内で延長する。
            // これにより、エディター専用のTimeline外挿設定を使わずに最終フレームを保持する。
            clip.duration = isChild || !isIncoming ? pose.end - original.start :
                Math.Min(original.duration, pose.end - original.start);
            clip.timeScale = original.timeScale;
            clip.easeInDuration = original.easeInDuration;
            clip.easeOutDuration = original.easeOutDuration;
            clip.blendInDuration = original.blendInDuration;
            clip.blendOutDuration = original == pose ? 0d : original.blendOutDuration;
            clip.mixInCurve = new AnimationCurve(original.mixInCurve.keys);
            clip.mixOutCurve = new AnimationCurve(original.mixOutCurve.keys);
            var asset = (AnimationPlayableAsset)clip.asset;
            CopyAnimationSettings(animation, asset);
            if (original.start > 0d && (isChild || !isIncoming))
            {
                // 元の移動記録は開始時刻0.1167秒より前を保持するため、実行時に低速の導入クリップを加え、
                // 最初のフレームの配置を維持する。
                TimelineClip leadIn = track.CreateClip(asset.clip);
                runtimeObjects.Add(leadIn.asset);
                leadIn.start = 0d;
                leadIn.duration = original.start;
                leadIn.clipIn = original.clipIn;
                leadIn.timeScale = 0.001d;
                CopyAnimationSettings(animation, (AnimationPlayableAsset)leadIn.asset);
            }
        }
        foreach (TrackAsset child in sourceTrack.GetChildTracks())
            if (child is AnimationTrack animation) CopyTrack(timeline, track, animation, isIncoming, true, isCamera);
        return track;
    }

    private static void CopyAnimationSettings(AnimationPlayableAsset sourceAsset, AnimationPlayableAsset copy)
    {
        copy.position = sourceAsset.position;
        copy.rotation = sourceAsset.rotation;
        copy.useTrackMatchFields = sourceAsset.useTrackMatchFields;
        copy.matchTargetFields = sourceAsset.matchTargetFields;
        copy.removeStartOffset = sourceAsset.removeStartOffset;
        copy.applyFootIK = sourceAsset.applyFootIK;
        copy.loop = AnimationPlayableAsset.LoopMode.Off;
    }

    private AnimationClip CloneAnimation(AnimationClip original)
    {
        AnimationClip copy = Object.Instantiate(original);
        copy.events = Array.Empty<AnimationEvent>();
        runtimeObjects.Add(copy);
        return copy;
    }

    private void OnSceneLoaded(Scene loaded, LoadSceneMode mode)
    {
        if (loaded.name != SceneName) { return; }
        scene = loaded;
        loading = false;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (disposed)
        {
            foreach (GameObject root in scene.GetRootGameObjects()) root.SetActive(false);
            QueueUnload();
        }
    }

    private void QueueUnload()
    {
        if (!ownsScene || !scene.IsValid() || !scene.isLoaded) { return; }
        ownsScene = false;
        // 呼び出し元のゲームシーンがなくなっても、常駐ローダーが後片付けを完了する。
        if (SceneLoader.Instance != null && SceneLoader.Instance.IsInitialized) SceneLoader.Unload(SceneName);
        else SceneManager.UnloadSceneAsync(scene);
    }

    public void Dispose()
    {
        if (disposed) { return; }
        disposed = true;
        if (!loading) SceneManager.sceneLoaded -= OnSceneLoaded;
        if (director != null) director.Stop();
        if (actorMoved && incoming != null)
        {
            int poseIndex = incomingAnimator != null ? incomingAnimator.GetInteger("StartPoseIndex") : 0;
            incoming.gameObject.SetActive(false);
            if (incomingAnimator != null)
            {
                incomingAnimator.applyRootMotion = originalRootMotion;
                incomingAnimator.cullingMode = originalCulling;
            }
            incoming.transform.SetParent(null, true);
            if (originalScene.IsValid() && originalScene.isLoaded)
            {
                SceneManager.MoveGameObjectToScene(incoming.gameObject, originalScene);
                if (originalParent != null) incoming.transform.SetParent(originalParent, false);
                incoming.transform.localScale = originalScale;
                incoming.transform.SetPositionAndRotation(destination, Quaternion.identity);
                incoming.gameObject.SetActive(completed);
                if (completed && incomingAnimator != null)
                {
                    incomingAnimator.Rebind();
                    incomingAnimator.SetInteger("StartPoseIndex", poseIndex);
                    incomingAnimator.Play("Idle", 0, 0f);
                    incomingAnimator.Update(0f);
                    incoming.transform.SetPositionAndRotation(destination, Quaternion.identity);
                    incoming.transform.localScale = originalScale;
                }
            }
            else Object.Destroy(incoming.gameObject);
        }
        foreach (GameObject root in scene.IsValid() && scene.isLoaded ? scene.GetRootGameObjects() : Array.Empty<GameObject>())
            root.SetActive(false);
        if (ownsStageOverride && stage != null) stage.EndThreePlayerPresentation();
        if (gameplayCamera != null) gameplayCamera.enabled = cameraWasEnabled;
        if (gameplayListener != null) gameplayListener.enabled = listenerWasEnabled;
        foreach (Object item in runtimeObjects) if (item != null) Object.Destroy(item);
        runtimeObjects.Clear();
        QueueUnload();
    }
}
