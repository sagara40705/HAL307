using UnityEngine;
using UnityEngine.Events;

public class RetireButtonAction : MonoBehaviour
{
    [Header("Retire Event")]
    [SerializeField] private UnityEvent onRetaire;
    private bool isTransitionRequested;

    public void OnClickRetire()
    {
        if (isTransitionRequested)
        {
            return;
        }

        GamePhaseManager phaseManager = GamePhaseManager.Instance;
        if (phaseManager != null && phaseManager.IsInitialized)
        {
            // The phase manager closes pause, restores time and cleans up scene audio.
            isTransitionRequested = phaseManager.ChangePhase(GamePhase.Title);
            return;
        }

        // Keep the standalone Pause scene usable without the Boot managers.
        GamePhaseSceneSettings settings = GamePhaseSceneSettings.Load();
        string titleScene = settings != null && settings.TryGetSceneName(GamePhase.Title, out string sceneName)
            ? sceneName
            : "Title";
        isTransitionRequested = SceneLoader.Load(titleScene);
        if (isTransitionRequested)
        {
            Time.timeScale = 1f;
        }
    }
}
