using UnityEngine;
using Player;
using Pose;
using TaiyouVoiceAI;

public class VoiceCommandAction : MonoBehaviour
{
    [Header("Menu")]
    //[SerializeField] private VoiceMenuController menuController;

    [Header("Player Pose")]
    [SerializeField] private VoiceAiContext voiceAiContext;
    [SerializeField] private PlayerPoseContoroller playerPoseController;

    [Header("Legacy Objects")]
    [SerializeField] private GameObject powerObject;
    [SerializeField] private GameObject yaaObject;
    [SerializeField] private GameObject muscleObject;

    [Header("Voice Gate")]
    [SerializeField] private float minimumDb = -60f;
    [SerializeField] private bool onlyHide = true;
    [SerializeField] private bool logCommand = true;

    private void Awake()
    {
        ResolvePlayerPoseController();
    }

    public void ExecuteCommand(string command, float volume)
    {
        Debug.Log($"[ActionTrace] VoiceCommandAction.ExecuteCommand entered. command={command}, volume={volume:F8}, object={name}, scene={gameObject.scene.name}, activeSelf={gameObject.activeSelf}, activeInHierarchy={gameObject.activeInHierarchy}, enabled={enabled}");

        if (string.IsNullOrWhiteSpace(command) || command == "none")
        {
            Debug.Log("[ActionTrace] VoiceCommandAction return. reason=empty command or none");
            return;
        }

        float db = VolumeToDb(volume);

        Debug.Log($"[ActionTrace] VoiceCommandAction gate. command={command}, db={db:F2}, minimumDb={minimumDb:F2}, belowMinimum={db < minimumDb}");
        if (db < minimumDb)
        {
            if (logCommand)
            {
                Debug.Log($"[Voice] ignored small input command={command} db={db:F1}");
            }
            Debug.Log("[ActionTrace] VoiceCommandAction return. reason=db below minimumDb");
            return;
        }

        if (logCommand)
        {
            Debug.Log($"[Voice] command={command} volume={volume:F4} db={db:F1}");
        }

        if (IsMenuCommand(command))
        {
            Debug.Log($"[ActionTrace] VoiceCommandAction return. reason=menu command command={command}");
            //menuController.ExecuteVoiceCommand(command);
            return;
        }

        if (IsPoseCommand(command) && !CanExecutePoseCommand(command))
        {
            return;
        }

        switch (command)
        {
            case "power":
                Debug.Log("[ActionTrace] VoiceCommandAction branch=power");
                TryExecutePose(PoseID.Pose_Power);
                HideOrDestroy(powerObject, "power");
                break;
            case "yaa":
                Debug.Log("[ActionTrace] VoiceCommandAction branch=yaa");
                TryExecutePose(PoseID.Pose_Yaa);
                HideOrDestroy(yaaObject, "yaa");
                break;
            case "muscle":
                Debug.Log("[ActionTrace] VoiceCommandAction branch=muscle");
                TryExecutePose(PoseID.Pose_Muscle);
                HideOrDestroy(muscleObject, "muscle");
                break;
            default:
                Debug.Log($"[ActionTrace] VoiceCommandAction switch default. command={command}");
                break;
        }
    }

    private void TryExecutePose(PoseID poseID)
    {
        ResolvePlayerPoseController();

        if (playerPoseController == null)
        {
            Debug.LogWarning($"[ActionTrace] PlayerPoseContoroller is not assigned. pose={poseID}");
            return;
        }

        Debug.Log($"[ActionTrace] PlayerPoseContoroller.TryOnPose before. pose={poseID}, target={playerPoseController.name}, scene={playerPoseController.gameObject.scene.name}, activeSelf={playerPoseController.gameObject.activeSelf}, activeInHierarchy={playerPoseController.gameObject.activeInHierarchy}, enabled={playerPoseController.enabled}");
        playerPoseController.TryOnPose(poseID);
        Debug.Log($"[ActionTrace] PlayerPoseContoroller.TryOnPose after. pose={poseID}");
    }

    private void ResolvePlayerPoseController()
    {
        if (playerPoseController != null)
        {
            return;
        }

        if (voiceAiContext == null)
        {
            voiceAiContext = FindFirstObjectByType<VoiceAiContext>();
        }

        playerPoseController = voiceAiContext?.GetPlayerContext()?.GetPlayerPoseContoroller;
    }

    private static bool IsMenuCommand(string command)
    {
        return command == "ue" || command == "sita" || command == "kettei" || command == "time";
    }

    private static bool IsPoseCommand(string command)
    {
        return command == "power" || command == "yaa" || command == "muscle";
    }

    private bool CanExecutePoseCommand(string command)
    {
        GamePhaseManager gamePhaseManager = GamePhaseManager.Instance;
        PauseManager pauseManager = PauseManager.Instance;

        if ((gamePhaseManager != null && gamePhaseManager.IsInitialized && gamePhaseManager.IsChangingPhase) ||
            (pauseManager != null && pauseManager.IsApplyingPauseView))
        {
            Debug.Log($"[VoiceCommandBlocked] command={command} reason=SceneTransition");
            return false;
        }

        if ((gamePhaseManager != null && gamePhaseManager.IsInitialized && gamePhaseManager.CurrentPhase == GamePhase.Pause) ||
            (pauseManager != null && pauseManager.IsPaused))
        {
            Debug.Log($"[VoiceCommandBlocked] command={command} reason=Pause");
            return false;
        }

        if (gamePhaseManager == null || !gamePhaseManager.IsInitialized)
        {
            Debug.Log($"[VoiceCommandBlocked] command={command} reason=GameStateUnavailable");
            return false;
        }

        if (gamePhaseManager.CurrentPhase != GamePhase.InGame)
        {
            Debug.Log($"[VoiceCommandBlocked] command={command} reason=NotInGame phase={gamePhaseManager.CurrentPhase}");
            return false;
        }

        ResolvePlayerPoseController();
        if (playerPoseController == null || !playerPoseController.isActiveAndEnabled)
        {
            Debug.Log($"[VoiceCommandBlocked] command={command} reason=PlayerUnavailable");
            return false;
        }

        return true;
    }

    private void HideOrDestroy(GameObject obj, string label)
    {
        if (obj == null)
        {
            Debug.LogWarning(label + " target is not assigned.");
            Debug.LogWarning($"[ActionTrace] HideOrDestroy skipped. label={label}, target=null");
            return;
        }

        Debug.Log($"[ActionTrace] HideOrDestroy target. label={label}, target={obj.name}, scene={obj.scene.name}, activeSelf={obj.activeSelf}, activeInHierarchy={obj.activeInHierarchy}, onlyHide={onlyHide}");

        if (onlyHide)
        {
            obj.SetActive(false);
            Debug.Log(label + " hidden.");
            Debug.Log($"[ActionTrace] HideOrDestroy after hide. label={label}, target={obj.name}, activeSelf={obj.activeSelf}, activeInHierarchy={obj.activeInHierarchy}");
        }
        else
        {
            Destroy(obj);
            Debug.Log(label + " destroyed.");
            Debug.Log($"[ActionTrace] HideOrDestroy after destroy requested. label={label}, target={obj.name}");
        }
    }

    private float VolumeToDb(float volume)
    {
        return 20f * Mathf.Log10(Mathf.Max(volume, 0.000001f));
    }
}
