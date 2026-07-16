using UnityEngine;
using static Sound.Manager.SoundManager;

public class TitleSceneMgr : MonoBehaviour
{
    [SerializeField] private GamePhase _nextPhase = GamePhase.InGame;
    [SerializeField] private string _nextSceneName = "Prototype";
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
            if (!gamePhaseManager.ChangePhase(_nextPhase))
            {
                Debug.LogWarning($"[TitleSceneMgr] Phase change was skipped. Phase={_nextPhase}");
            }

            return;
        }

        string sceneName = ResolveFallbackSceneName();
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError($"[TitleSceneMgr] Scene load failed. Reason=Scene name is empty. Phase={_nextPhase}");
            return;
        }
        Sound.Manager.SoundManager.Stop((int)SceneSoundTag.TitleSound, (int)TitleSoundTag.TitleBGM);

        SceneLoader.Load(sceneName);
    }

    private string ResolveFallbackSceneName()
    {
        GamePhaseSceneSettings settings = GamePhaseSceneSettings.Load();
        if (settings != null && settings.TryGetSceneName(_nextPhase, out string sceneName))
        {
            return sceneName;
        }

        return _nextSceneName;
    }
}
