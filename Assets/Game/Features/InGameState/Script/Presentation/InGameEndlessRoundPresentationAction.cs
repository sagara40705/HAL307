using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// Endless composition of existing intro Actions and animation clips. No shared Timeline is edited.
[CreateAssetMenu(fileName = "EndlessRoundPresentation", menuName = "Game/InGame/Presentation/Endless Round")]
public sealed class InGameEndlessRoundPresentationAction : InGamePresentationAction
{
    [SerializeField] private InGamePresentationAction initialEntrance;
    [SerializeField] private AnimationClip npcClimb;
    [SerializeField] private AnimationClip[] finishPoses;
    private Action cancelPlayback;

    public void CancelPlayback()
    {
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
    }

    public override IEnumerator Play(InGameFlowContext context)
    {
        ValidateConfiguration();
        yield return initialEntrance.Play(context);
    }

    public IEnumerator PlayFinish(Player.PlayerPoseContoroller player)
    {
        AnimationClip clip = finishPoses[UnityEngine.Random.Range(0, finishPoses.Length)];
        yield return PlayClip(player.PoseAnimator, clip);
    }

    public IEnumerator PlayIncoming(global::Enemy.Enemy enemy, Vector3 destination)
    {
        // Use the existing climb clip on the real pending NPC, rather than creating a second actor.
        Vector3 start = destination + Vector3.down * 3f;
        enemy.transform.position = start;
        enemy.transform.rotation = Quaternion.identity;
        enemy.gameObject.SetActive(true);
        yield return PlayClip(enemy.GetComponentInChildren<Animator>(), npcClimb,
            progress => { if (enemy != null) { enemy.transform.position = Vector3.Lerp(start, destination, progress); } });
        if (enemy == null) { throw new InvalidOperationException("[Endless] Incoming NPC was destroyed during its entrance."); }
        enemy.transform.position = destination;
    }

    private IEnumerator PlayClip(Animator animator, AnimationClip clip, Action<float> onProgress = null)
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
        PlayableGraph graph = PlayableGraph.Create("Endless " + clip.name);
        bool released = false;
        Action release = () =>
        {
            if (released) { return; }
            released = true;
            if (graph.IsValid()) { graph.Destroy(); }
            if (animator == null) { return; }
            animator.applyRootMotion = originalRootMotion;
            animator.cullingMode = originalCulling;
            animator.speed = originalSpeed;
            // The gameplay root moves to its Slot; model-local animation offsets must be restored.
            if (onProgress == null || animator.GetComponent<global::Enemy.Enemy>() == null)
            {
                animator.transform.localPosition = localPosition;
                animator.transform.localRotation = localRotation;
                animator.transform.localScale = localScale;
            }
            animator.GetComponent<OnStartPose>()?.SetTriggerIdle();
        };
        CancelPlayback();
        cancelPlayback = release;
        try
        {
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.speed = 1f;
            AnimationClipPlayable playable = AnimationClipPlayable.Create(graph, clip);
            playable.SetDuration(clip.length);
            playable.SetApplyFootIK(false);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(graph, "Actor", animator);
            output.SetSourcePlayable(playable);
            graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
            graph.Play();

            float elapsed = 0f;
            while (playable.GetTime() < clip.length || Time.timeScale <= 0f)
            {
                if (animator == null || !animator.isActiveAndEnabled)
                {
                    throw new InvalidOperationException("[Endless] Presentation actor became unavailable.");
                }

                if (Time.timeScale > 0f)
                {
                    onProgress?.Invoke(Mathf.Clamp01((float)playable.GetTime() / clip.length));
                }
                elapsed += Time.deltaTime;
                if (elapsed > Mathf.Max(30f, clip.length * 3f))
                {
                    throw new InvalidOperationException("[Endless] Animation playback stopped advancing: " + clip.name);
                }
                yield return null;
            }
            onProgress?.Invoke(1f);
        }
        finally
        {
            release();
            if (cancelPlayback == release) { cancelPlayback = null; }
        }
    }
}
