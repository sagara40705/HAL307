using UnityEngine;

public class StageSceneMgr : MonoBehaviour
{
    [SerializeField] private GamePhase _nextPhase = GamePhase.Result;
    [SerializeField] private string _nextSceneName = "Result";

    public void GoResultScene()
    {
        GamePhaseManager gamePhaseManager = GamePhaseManager.Instance;
        if (gamePhaseManager != null && gamePhaseManager.IsInitialized)
        {
            gamePhaseManager.ChangePhase(_nextPhase);
            return;
        }

        SceneLoader.Load(_nextSceneName);
    }
}
