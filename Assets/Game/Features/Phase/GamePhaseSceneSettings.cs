using System;
using System.Collections.Generic;
using UnityEngine;

public enum GamePhase
{
    None = 0,
    Title = 2,
    InGame = 3,
    Result = 4,
    Pause = 5,
}

[CreateAssetMenu(menuName = "HAL/Game Phase/Game Phase Scene Settings")]
public sealed class GamePhaseSceneSettings : ScriptableObject
{
    public const string ResourcesName = "GamePhaseSceneSettings";

    [SerializeField] private List<GamePhaseSceneBinding> phaseScenes = new();

    public IReadOnlyList<GamePhaseSceneBinding> PhaseScenes => phaseScenes;

    public static GamePhaseSceneSettings Load()
    {
        return Resources.Load<GamePhaseSceneSettings>(ResourcesName);
    }

    public bool TryGetSceneName(GamePhase phase, out string sceneName)
    {
        for (int i = 0; i < phaseScenes.Count; i++)
        {
            GamePhaseSceneBinding binding = phaseScenes[i];
            if (binding == null || binding.Phase != phase)
            {
                continue;
            }

            sceneName = binding.SceneName;
            return !string.IsNullOrWhiteSpace(sceneName);
        }

        sceneName = string.Empty;
        return false;
    }

    public bool TryGetPhaseBySceneName(string sceneName, out GamePhase phase)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            phase = GamePhase.None;
            return false;
        }

        string normalizedSceneName = SceneLookup.NormalizeName(sceneName);

        for (int i = 0; i < phaseScenes.Count; i++)
        {
            GamePhaseSceneBinding binding = phaseScenes[i];
            if (binding == null)
            {
                continue;
            }

            if (string.Equals(SceneLookup.NormalizeName(binding.SceneName), normalizedSceneName, StringComparison.Ordinal))
            {
                phase = binding.Phase;
                return phase != GamePhase.None;
            }
        }

        phase = GamePhase.None;
        return false;
    }

}

[Serializable]
public sealed class GamePhaseSceneBinding
{
    [SerializeField] private GamePhase phase;
    [SerializeField] private string sceneName;

    public GamePhase Phase => phase;
    public string SceneName => sceneName;
}
