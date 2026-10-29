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
            // フェーズ管理側でポーズメニューを閉じ、時間を復元してシーンの音声を片付ける。
            isTransitionRequested = phaseManager.ChangePhase(GamePhase.Title);
            return;
        }

        // BootのManagerがなくても、Pauseシーン単体で使用できるようにする。
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
