using System.Collections;
using UnityEngine;

public sealed class BootSceneRouter : MonoBehaviour
{
    [SerializeField] private AppBootstrapper appBootstrapper;

    [Header("Build時の遷移先。Scene名")]
    [SerializeField] private string defaultFirstSceneName = "Prototype";

#if UNITY_EDITOR
    private const string TargetSceneKey = "PlayFromBoot.TargetScenePath";
#endif

    private IEnumerator Start()
    {
        if (appBootstrapper == null)
        {
            Debug.LogError("[BootSceneRouter] AppBootstrapper is not assigned.");
            yield break;
        }

        yield return appBootstrapper.RunBootSequence();

        if (appBootstrapper.HasFailed)
        {
            Debug.LogError("[BootSceneRouter] Boot failed. Scene transition was cancelled.");
            yield break;
        }

        string nextSceneName = GetNextSceneName();
        if (string.IsNullOrEmpty(nextSceneName))
        {
            Debug.LogError("[BootSceneRouter] Next scene name is empty.");
            yield break;
        }

        if (!Application.CanStreamedLevelBeLoaded(nextSceneName))
        {
            Debug.LogError($"[BootSceneRouter] Scene transition failed. Reason=Scene cannot be loaded. Scene='{nextSceneName}'. Check Build Settings scene list or Build Profile scene list.");
            yield break;
        }

        SceneLoader sceneLoader = SceneLoader.Instance;
        if (sceneLoader == null || !sceneLoader.IsInitialized)
        {
            Debug.LogError($"[BootSceneRouter] Scene transition failed. Reason=SceneLoader is not ready. Scene='{nextSceneName}'");
            yield break;
        }

        yield return sceneLoader.LoadAsync(nextSceneName);
    }

    private string GetNextSceneName()
    {
#if UNITY_EDITOR
        string editorTarget = UnityEditor.SessionState.GetString(TargetSceneKey, string.Empty);

        if (!string.IsNullOrEmpty(editorTarget))
        {
            UnityEditor.SessionState.EraseString(TargetSceneKey);
            return editorTarget;
        }
#endif
        return defaultFirstSceneName;
    }
}
