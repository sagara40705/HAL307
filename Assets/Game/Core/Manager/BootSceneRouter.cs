using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class BootSceneRouter : MonoBehaviour
{
    [SerializeField] private AppBootstrapper appBootstrapper;

    [Header("Build時の遷移先。フルパス")]
    [SerializeField] private string defaultFirstScenePath = "Assets/Scenes/Title.unity";

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

        string nextScenePath = GetNextScenePath();
        if (string.IsNullOrEmpty(nextScenePath))
        {
            Debug.LogError("[BootSceneRouter] Next scene path is empty.");
            yield break;
        }

        yield return SceneManager.LoadSceneAsync(nextScenePath, LoadSceneMode.Single);
    }

    private string GetNextScenePath()
    {
#if UNITY_EDITOR
        string editorTarget = UnityEditor.SessionState.GetString(TargetSceneKey, string.Empty);

        if (!string.IsNullOrEmpty(editorTarget))
        {
            UnityEditor.SessionState.EraseString(TargetSceneKey);
            return editorTarget;
        }
#endif
        return defaultFirstScenePath;
    }
}