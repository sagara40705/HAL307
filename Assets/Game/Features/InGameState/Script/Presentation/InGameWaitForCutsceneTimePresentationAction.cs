using System.Collections;
using UnityEngine;
using UnityEngine.Playables;

// CutsceneのTimelineが指定時間へ到達するまで待機する
[CreateAssetMenu(
    fileName = "InGameWaitForCutsceneTimePresentationAction",
    menuName = "Game/InGame/Presentation/Wait For Cutscene Time")]
public sealed class InGameWaitForCutsceneTimePresentationAction : InGamePresentationAction
{
    [SerializeField, Min(0f)] private float targetTime = 4.0f;

    public override IEnumerator Play(InGameFlowContext context)
    {
        CutsceneTimelineController controller = FindController();
        if (controller == null)
        {
            Debug.LogWarning("[InGameWaitForCutsceneTimePresentationAction] Wait skipped. Reason=CutsceneTimelineController was not found.");
            yield break;
        }

        PlayableDirector director = controller.Director;
        if (director == null)
        {
            Debug.LogWarning("[InGameWaitForCutsceneTimePresentationAction] Wait skipped. Reason=PlayableDirector was not found.", controller);
            yield break;
        }

        double endTime = Mathf.Max(0f, targetTime);
        while (director != null && director.time < endTime)
        {
            if (director.state != PlayState.Playing)
            {
                Debug.LogWarning(
                    $"[InGameWaitForCutsceneTimePresentationAction] Wait ended before target time. Current={director.time:F2}, Target={endTime:F2}",
                    controller);
                yield break;
            }

            yield return null;
        }
    }

    private static CutsceneTimelineController FindController()
    {
        CutsceneTimelineController[] controllers = Object.FindObjectsByType<CutsceneTimelineController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (CutsceneTimelineController controller in controllers)
        {
            if (controller != null && controller.Director != null)
            {
                return controller;
            }
        }

        return null;
    }
}
