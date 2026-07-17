using System.Collections;
using Player;
using Pose;
using TaiyouVoiceAI;
using UnityEngine;

public class VoiceCommandAction : MonoBehaviour
{
    [Header("Menu")]
    //[SerializeField] private VoiceMenuController menuController;

    [Header("Legacy Objects")]
    [SerializeField] private GameObject powerObject;
    [SerializeField] private GameObject yaaObject;
    [SerializeField] private GameObject muscleObject;

    [Header("Voice Gate")]
    [SerializeField] private float minimumDb = -20f;
    [SerializeField] private bool onlyHide = true;
    [SerializeField] private float hideTime = 5f;
    [SerializeField] private bool logCommand = true;

    [Header("Player")]
    [SerializeField] private VoiceAiContext _voiceAiContext;
    [SerializeField] private PlayerPoseContoroller _playerPoseController;

    private void Awake()
    {
        ResolvePlayerPoseController();
    }

    private void Start()
    {
        if (!ResolvePlayerPoseController())
        {
            Debug.LogError("[VoiceCommandAction] PlayerPoseContoroller was not found through VoiceAiContext.");
        }
    }

    public void ExecuteCommand(string command, float volume)
    {
        if (string.IsNullOrWhiteSpace(command) || command == "none")
        {
            return;
        }

        command = command.Trim().ToLowerInvariant();

        float db = VolumeToDb(volume);
        if (db < minimumDb)
        {
            if (logCommand)
            {
                Debug.Log($"[Voice] ignored small input command={command} db={db:F1}");
            }
            return;
        }

        if (logCommand)
        {
            Debug.Log($"[Voice] command={command} volume={volume:F4} db={db:F1}");
        }

        if (IsMenuCommand(command))
        {
            //menuController.ExecuteVoiceCommand(command);
            return;
        }

        switch (command)
        {
            case "power":
                HideOrDestroy(powerObject, "power");
                TryStartPlayerPose(PoseID.Pose_Power, command);
                break;

            case "yaa":
                HideOrDestroy(yaaObject, "yaa");
                TryStartPlayerPose(PoseID.Pose_Yaa, command);
                break;

            case "muscle":
                HideOrDestroy(muscleObject, "muscle");
                TryStartPlayerPose(PoseID.Pose_Muscle, command);
                break;
        }
    }

    private bool TryStartPlayerPose(PoseID poseID, string command)
    {
        if (!ResolvePlayerPoseController())
        {
            Debug.LogWarning($"[VoiceCommandAction] Player pose skipped. Reason=PlayerPoseContoroller not found. Command={command}");
            return false;
        }

        if (InGameFlowLock.IsPresentationPlaying)
        {
            Debug.Log($"[VoiceCommandAction] Player pose skipped. Reason=Presentation is playing. Command={command}");
            return false;
        }

        if (_playerPoseController.IsPosing)
        {
            Debug.Log($"[VoiceCommandAction] Player pose skipped. Reason=Player is already posing. Command={command}");
            return false;
        }

        _playerPoseController.TryOnPose(poseID);
        Debug.Log($"[VoiceCommandAction] Player pose started. Command={command}, Pose={poseID}");
        return true;
    }

    private bool ResolvePlayerPoseController()
    {
        if (_playerPoseController != null)
        {
            return true;
        }

        if (_voiceAiContext == null)
        {
            _voiceAiContext = FindFirstObjectByType<VoiceAiContext>();
        }

        PlayerContext playerContext = _voiceAiContext?.GetPlayerContext();
        _playerPoseController = playerContext?.GetPlayerPoseContoroller;

        if (_playerPoseController == null)
        {
            _playerPoseController = FindFirstObjectByType<PlayerPoseContoroller>();
        }

        return _playerPoseController != null;
    }

    private static bool IsMenuCommand(string command)
    {
        return command == "ue" ||
               command == "sita" ||
               command == "kettei" ||
               command == "time";
    }

    private void HideOrDestroy(GameObject obj, string label)
    {
        if (obj == null)
        {
            Debug.LogWarning(label + " target is not assigned.");
            return;
        }

        if (onlyHide)
        {
            if (!obj.activeSelf)
            {
                return;
            }

            obj.SetActive(false);
            StartCoroutine(ShowAfterDelay(obj));

            if (logCommand)
            {
                Debug.Log(label + " hidden.");
            }
        }
        else
        {
            Destroy(obj);

            if (logCommand)
            {
                Debug.Log(label + " destroyed.");
            }
        }
    }

    private IEnumerator ShowAfterDelay(GameObject obj)
    {
        yield return new WaitForSeconds(hideTime);

        if (obj != null)
        {
            obj.SetActive(true);

            if (logCommand)
            {
                Debug.Log(obj.name + " shown.");
            }
        }
    }

    private float VolumeToDb(float volume)
    {
        return 20f * Mathf.Log10(Mathf.Max(volume, 0.000001f));
    }
}
