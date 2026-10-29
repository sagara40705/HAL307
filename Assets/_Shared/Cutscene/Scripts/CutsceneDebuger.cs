using UnityEngine;

public class CutsceneDebuger : MonoBehaviour
{
    [SerializeField] private CutsceneTimelineController _cutsceneTimelineController;


    // 毎フレーム呼び出される
    void Update()
    {
        if (DebugShortcuts.Pressed(UnityEngine.InputSystem.Key.Space))
        {
            Debug.Log("CutsceneDebuger:Try Play");
            _cutsceneTimelineController?.Play();
        }

        if (DebugShortcuts.Pressed(UnityEngine.InputSystem.Key.Escape))
        {
            Debug.Log("CutsceneDebuger:Try End");
            _cutsceneTimelineController?.EndCutScene();
        }
    }
}
