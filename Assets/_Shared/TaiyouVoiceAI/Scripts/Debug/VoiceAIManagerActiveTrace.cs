using UnityEngine;

public class VoiceAIManagerActiveTrace : MonoBehaviour
{
    [SerializeField] private GameObject target;
    [SerializeField] private string traceName = "VoiceAIManager";

    private bool hasPreviousState;
    private bool previousActiveSelf;
    private bool previousActiveInHierarchy;
    private bool previousSceneIsLoaded;

    private GameObject Target => target != null ? target : gameObject;

    private void OnEnable()
    {
        LogSelfLifecycle("OnEnable");
        LogWatcherState("OnEnable", force: true);
    }

    private void LateUpdate()
    {
        LogWatcherState("LateUpdate", force: false);
    }

    private void OnDisable()
    {
        LogSelfLifecycle("OnDisable");
        LogWatcherState("OnDisable", force: true);
    }

    private void LogSelfLifecycle(string point)
    {
        Debug.Log(
            $"[VoiceAIActiveTrace] " +
            $"{point} " +
            $"frame={Time.frameCount} " +
            $"time={Time.time:F3} " +
            $"activeSelf={gameObject.activeSelf} " +
            $"activeInHierarchy={gameObject.activeInHierarchy} " +
            $"scene={gameObject.scene.name} " +
            $"sceneIsLoaded={gameObject.scene.isLoaded} " +
            $"enabled={enabled}"
        );
    }

    private void LogWatcherState(string point, bool force)
    {
        GameObject currentTarget = Target;
        bool targetExists = currentTarget != null;
        bool activeSelf = targetExists && currentTarget.activeSelf;
        bool activeInHierarchy = targetExists && currentTarget.activeInHierarchy;
        bool sceneIsLoaded = targetExists && currentTarget.scene.isLoaded;

        bool changed = !hasPreviousState
            || previousActiveSelf != activeSelf
            || previousActiveInHierarchy != activeInHierarchy
            || previousSceneIsLoaded != sceneIsLoaded;

        if (force || changed)
        {
            Debug.Log(
                $"[VoiceAIWatcher] " +
                $"point={point} " +
                $"watcher={gameObject.name} " +
                $"traceName={traceName} " +
                $"frame={Time.frameCount} " +
                $"time={Time.time:F3} " +
                $"targetExists={targetExists} " +
                $"target={(targetExists ? currentTarget.name : "<null>")} " +
                $"activeSelf={activeSelf} " +
                $"activeInHierarchy={activeInHierarchy} " +
                $"scene={(targetExists ? currentTarget.scene.name : "<none>")} " +
                $"sceneIsLoaded={sceneIsLoaded}"
            );
        }

        hasPreviousState = true;
        previousActiveSelf = activeSelf;
        previousActiveInHierarchy = activeInHierarchy;
        previousSceneIsLoaded = sceneIsLoaded;
    }
}
