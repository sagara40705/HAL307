using System.Collections;
using UnityEngine;

public sealed class BootSceneRouter : MonoBehaviour
{
    [SerializeField] private AppBootstrapper appBootstrapper;

    [Header("First Phase")]
    [SerializeField] private GamePhase defaultFirstPhase = GamePhase.InGame;

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

        string editorTargetSceneName = GetEditorTargetSceneName();
        if (!string.IsNullOrEmpty(editorTargetSceneName))
        {
            yield return LoadEditorTargetScene(editorTargetSceneName);
            yield break;
        }

        GamePhaseManager gamePhaseManager = GamePhaseManager.Instance;
        if (gamePhaseManager == null || !gamePhaseManager.IsInitialized)
        {
            Debug.LogError("[BootSceneRouter] Scene transition failed. Reason=GamePhaseManager is not ready.");
            yield break;
        }

        if (!gamePhaseManager.ChangePhase(defaultFirstPhase))
        {
            Debug.LogError($"[BootSceneRouter] Scene transition failed. Reason=Phase change could not start. Phase={defaultFirstPhase}");
        }
    }

    private IEnumerator LoadEditorTargetScene(string sceneName)
    {
        GamePhaseManager gamePhaseManager = GamePhaseManager.Instance;
        if (gamePhaseManager == null || !gamePhaseManager.IsInitialized)
        {
            Debug.LogError($"[BootSceneRouter] Scene transition failed. Reason=GamePhaseManager is not ready. Scene='{sceneName}'");
            yield break;
        }

        if (!gamePhaseManager.LoadSceneAndSyncPhase(sceneName))
        {
            Debug.LogError($"[BootSceneRouter] Scene transition failed. Reason=Scene load could not start. Scene='{sceneName}'");
        }
    }

    private string GetEditorTargetSceneName()
    {
#if UNITY_EDITOR
        string editorTarget = UnityEditor.SessionState.GetString(TargetSceneKey, string.Empty);

        if (!string.IsNullOrEmpty(editorTarget))
        {
            UnityEditor.SessionState.EraseString(TargetSceneKey);
            return editorTarget;
        }
#endif
        return string.Empty;
    }
}
