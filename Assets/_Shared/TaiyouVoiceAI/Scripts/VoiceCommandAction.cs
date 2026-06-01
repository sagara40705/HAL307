using Player;
using UnityEngine;
using Context;
using Pose;
using TaiyouVoiceAI;

public class VoiceCommandAction : MonoBehaviour
{
    [Header("対象オブジェクト")]
    [SerializeField] private GameObject powerObject;
    [SerializeField] private GameObject yaaObject;
    [SerializeField] private GameObject muscleObject;

    [Header("音量設定")]
    [SerializeField] private float minVoiceVolume = 0.05f;

    [Header("設定")]
    [SerializeField] private bool onlyHide = true;
    [SerializeField] private bool logCommand = true;

    [Header("再表示設定")]
    [SerializeField] private float hideTime = 5f;

    [SerializeField] private VoiceAiContext _voiceAiContext;
    [SerializeField] private PlayerPoseContoroller _playerPoseController;

    private void Start()
    {
        _playerPoseController = _voiceAiContext.GetPlayerContext().GetPlayerPoseContoroller;
        if (_playerPoseController == null)
        {
            Debug.LogError("PlayerPoseControllerが見つかりません");
        }
    }

    public void ExecuteCommand(string command, float volume)
    {
        if (string.IsNullOrEmpty(command))
        {
            return;
        }

        if (logCommand)
        {
            Debug.Log("[Action] command = " + command +
                      " / volume = " + volume);
        }

        // 音量が小さい場合は無視
        if (volume < minVoiceVolume)
        {
            Debug.Log("声が小さいので無視 volume=" + volume);
            return;
        }

        switch (command)
        {
            case "power":
                HideOrDestroy(powerObject, "power");
                _playerPoseController?.TryOnPose(PoseID.Pose_Power);
              
                break;

            case "yaa":
                HideOrDestroy(yaaObject, "yaa");
                _playerPoseController?.TryOnPose(PoseID.Pose_Yaa);

                break;

            case "muscle":
                HideOrDestroy(muscleObject, "muscle");
                _playerPoseController?.TryOnPose(PoseID.Pose_Muscle);

                break;
        }

        if (_playerPoseController != null)
        {
            Debug.Log("/////////音声でポーズが開始されました//////////");
        }
        else
        {
            Debug.LogWarning("/////////PlayerPoseControllerが見つからないため、ポーズを開始できません//////////");
        }
    }

    private void HideOrDestroy(GameObject obj, string label)
    {
        if (obj == null)
        {
            Debug.LogWarning(label + " の対象オブジェクトが設定されていません");
            return;
        }

        if (onlyHide)
        {
            StartCoroutine(HideTemporarily(obj, label));
        }
        else
        {
            Destroy(obj);
            Debug.Log(label + " → 削除");
        }
    }

    private System.Collections.IEnumerator HideTemporarily(GameObject obj, string label)
    {
        obj.SetActive(false);

        Debug.Log(label + " → 非表示");

        yield return new WaitForSeconds(hideTime);

        if (obj != null)
        {
            obj.SetActive(true);

            Debug.Log(label + " → 再表示");
        }
    }
}
