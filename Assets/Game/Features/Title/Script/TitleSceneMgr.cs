using UnityEngine;

public class TitleSceneMgr : MonoBehaviour
{
    [SerializeField] private GamePhase _nextPhase = GamePhase.InGame;
    [SerializeField] private string _nextSceneName = "GameScene";
    [SerializeField] private UnityEngine.InputSystem.Key _key = UnityEngine.InputSystem.Key.None;

    private void Update()
    {
        if (DebugShortcuts.Pressed(_key))
        {
            StartLoad();
        }
    }


    private void StartLoad()
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
