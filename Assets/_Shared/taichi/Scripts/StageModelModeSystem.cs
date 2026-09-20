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

    [NonSerialized] private bool _isPresentationOverride;
    [NonSerialized] private bool _stage3PWasActive;
    [NonSerialized] private bool _stage5PWasActive;

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

        if (!ValidateStageObjects(scene))
        {
            return false;
        }

        bool useFivePlayerStage = gameMode == GameMode.FivePlayer && !_isPresentationOverride;
        stage3P.SetActive(!useFivePlayerStage);
        stage5P.SetActive(useFivePlayerStage);

        Debug.Log($"[StageModelModeSystem] Stage switched. Mode={gameMode}, Stage3PActive={stage3P.activeSelf}, Stage5PActive={stage5P.activeSelf}");
        return true;
    }

    // ゲームモードを変更せず、5Pの演出中だけ3Pモデルを表示
    public bool BeginThreePlayerPresentation(Scene scene)
    {
        GameModeManager manager = GameModeManager.Instance;
        if (_isPresentationOverride || manager == null || !manager.IsInitialized ||
            manager.CurrentGameMode != GameMode.FivePlayer)
        {
            return false;
        }

        ResolveStageObjects(scene);
        if (!ValidateStageObjects(scene))
        {
            return false;
        }

        _stage3PWasActive = stage3P.activeSelf;
        _stage5PWasActive = stage5P.activeSelf;
        _isPresentationOverride = true;
        stage3P.SetActive(true);
        stage5P.SetActive(false);
        return true;
    }

    // 演出開始直前の有効状態を復元。多重終了は無視
    public void EndThreePlayerPresentation()
    {
        if (!_isPresentationOverride)
        {
            return;
        }

        _isPresentationOverride = false;
        if (stage5P != null)
        {
            stage5P.SetActive(_stage5PWasActive);
        }
        if (stage3P != null)
        {
            stage3P.SetActive(_stage3PWasActive);
        }
    }

    private bool ValidateStageObjects(Scene scene)
    {
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
