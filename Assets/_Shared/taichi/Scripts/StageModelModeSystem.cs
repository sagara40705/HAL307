using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// StageModelシーン内のゲームモード別オブジェクトを切り替える。
/// </summary>
[Serializable]
public sealed class StageModelModeSystem
{
    private const string Stage3PObjectName = "Stage_3P";
    private const string Stage5PObjectName = "Stage_5P";
    private const string LegacyStage3PObjectName = "SM_FullStage";
    private const string LegacyStage5PObjectName = "SM_FullStage_5P";

    [SerializeField] private GameObject stage3P;
    [SerializeField] private GameObject stage5P;

    public bool Apply(Scene scene)
    {
        ResolveStageObjects(scene);

        GameModeManager gameModeManager = GameModeManager.Instance;
        GameMode gameMode = GameMode.ThreePlayer;
        if (gameModeManager != null && gameModeManager.IsInitialized)
        {
            gameMode = gameModeManager.CurrentGameMode;
            gameModeManager.LockSelection();
        }
        else
        {
            Debug.LogWarning("[StageModelModeSystem] GameModeManager is not ready. ThreePlayer mode is used.");
        }

        if (stage3P == null || stage5P == null)
        {
            Debug.LogError($"[StageModelModeSystem] Stage switch failed. Reason=Stage object was not found. Scene='{scene.name}', Stage3P={stage3P}, Stage5P={stage5P}");
            return false;
        }

        if (stage3P == stage5P)
        {
            Debug.LogError("[StageModelModeSystem] Stage switch failed. Reason=Stage3P and Stage5P reference the same object.");
            return false;
        }

        bool useFivePlayerStage = gameMode == GameMode.FivePlayer;
        stage3P.SetActive(!useFivePlayerStage);
        stage5P.SetActive(useFivePlayerStage);

        Debug.Log($"[StageModelModeSystem] Stage switched. Mode={gameMode}, Stage3PActive={stage3P.activeSelf}, Stage5PActive={stage5P.activeSelf}");
        return true;
    }

    private void ResolveStageObjects(Scene scene)
    {
        if (stage3P == null)
        {
            stage3P = FindSceneObject(scene, Stage3PObjectName) ??
                      FindSceneObject(scene, LegacyStage3PObjectName);
        }

        if (stage5P == null)
        {
            stage5P = FindSceneObject(scene, Stage5PObjectName) ??
                      FindSceneObject(scene, LegacyStage5PObjectName);
        }
    }

    private static GameObject FindSceneObject(Scene scene, string objectName)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return null;
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform target in transforms)
            {
                if (string.Equals(target.name, objectName, StringComparison.Ordinal))
                {
                    return target.gameObject;
                }
            }
        }

        return null;
    }
}
