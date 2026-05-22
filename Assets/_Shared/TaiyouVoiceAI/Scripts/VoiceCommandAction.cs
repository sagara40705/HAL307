using UnityEngine;
using TaiyouVoiceAI;
using UnityEngine.InputSystem;

public class VoiceCommandAction : MonoBehaviour
{
    [Header("対象オブジェクト")]
    [SerializeField] private GameObject powerObject;
    [SerializeField] private GameObject yaaObject;
    [SerializeField] private GameObject muscleObject;

    [Header("設定")]
    [SerializeField] private bool onlyHide = true;
    [SerializeField] private bool logCommand = true;

    [Header("Context")]
    [SerializeField] private VoiceAiContext voiceAiContext;
    private Player.PlayerPoseContoroller playerPoseController;

    private void Start()
    {
        playerPoseController = voiceAiContext?.GetPlayerContext()?.GetPlayerPoseContoroller;
        Debug.Log("VoiceCommandAction Start. playerPoseController = " + playerPoseController);
    }

    private void Update()
    {
        if (DebugShortcuts.Combo(Key.P, Key.Space))
        {
            playerPoseController?.TryOnPose(Pose.PoseID.Pose_Power);
        }
    }

    public void ExecuteCommand(string command)
    {
        if (string.IsNullOrEmpty(command))
        {
            return;
        }

        if (logCommand)
        {
            //Debug.Log("[Action] command = " + command);
        }

        switch (command)
        {
            case "power":
                HandleObject(powerObject, "power");
                playerPoseController?.TryOnPose(Pose.PoseID.Pose_Power);
                break;

            case "yaa":
                HandleObject(yaaObject, "yaa");
                playerPoseController?.TryOnPose(Pose.PoseID.Pose_Yaa);
                break;

            case "muscle":
                HandleObject(muscleObject, "muscle");
                playerPoseController?.TryOnPose(Pose.PoseID.Pose_Muscle);
                break;

            default:
                // none や不明は何もしない
                break;
        }
    }

    private void HandleObject(GameObject obj, string label)
    {
        if (obj == null)
        {
            Debug.LogWarning(label + " の対象オブジェクトが設定されていません");
            return;
        }

        if (onlyHide)
        {
            obj.SetActive(false);
        }
        else
        {
            Destroy(obj);
        }
    }
}
