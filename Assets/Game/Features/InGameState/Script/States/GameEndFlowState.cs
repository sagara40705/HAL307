using System.Collections;
using UnityEngine;

// ゲーム終了演出とリザルト遷移State
public sealed class GameEndFlowState : InGameFlowStateBase
{
    private readonly StageData _stageData;

    public GameEndFlowState(StageData stageData)
    {
        _stageData = stageData;
    }

    // ゲーム終了演出の実行とリザルトシーン読み込み
    public override IEnumerator Execute(InGameFlowContext context)
    {
        Debug.Log("[GameEndFlowState] Game end started.");

        InGamePresentationAction action = _stageData != null ? _stageData.GameEndAction : null;
        if (action != null)
        {
            Debug.Log($"[GameEndFlowState] Game end action started. Action={action.name}");
            InGameFlowLock.BeginPresentation();
            try
            {
                yield return action.Play(context);
            }
            finally
            {
                InGameFlowLock.EndPresentation();
            }

            Debug.Log($"[GameEndFlowState] Game end action finished. Action={action.name}");
        }

        GamePhaseManager gamePhaseManager = GamePhaseManager.Instance;
        if (gamePhaseManager != null && gamePhaseManager.IsInitialized)
        {
            Debug.Log("[GameEndFlowState] Changing phase to Result.");
            yield return gamePhaseManager.ChangePhaseAsync(GamePhase.Result);
            yield break;
        }

        string resultSceneName = _stageData != null ? _stageData.ResultSceneName : string.Empty;
        if (string.IsNullOrWhiteSpace(resultSceneName))
        {
            Debug.LogError("[GameEndFlowState] Result scene load failed. Reason=ResultSceneName is empty.");
            yield break;
        }

        if (!Application.CanStreamedLevelBeLoaded(resultSceneName))
        {
            Debug.LogError($"[GameEndFlowState] Result scene load failed. Reason=Scene cannot be loaded. Scene='{resultSceneName}'. Check Build Settings scene list.");
            yield break;
        }

        SceneLoader sceneLoader = SceneLoader.Instance;
        if (sceneLoader == null || !sceneLoader.IsInitialized)
        {
            Debug.LogError($"[GameEndFlowState] Result scene load failed. Reason=SceneLoader is not ready. Scene='{resultSceneName}'");
            yield break;
        }

        Debug.Log($"[GameEndFlowState] Loading result scene. Scene='{resultSceneName}'");
        yield return sceneLoader.LoadAsync(resultSceneName);
    }
}
