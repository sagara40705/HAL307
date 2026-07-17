using System.Collections;
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
    [SerializeField] private float minimumDb = -50f;
    [SerializeField] private bool onlyHide = true;
    [SerializeField] private float hideTime = 5f;
    [SerializeField] private bool logCommand = true;

    public void ExecuteCommand(string command, float volume)
    {
        // 空文字だけ無視する
        // noneは下のswitchで処理するため、ここでは止めない
        if (string.IsNullOrWhiteSpace(command))
        {
            return;
        }

        float db = VolumeToDb(volume);

        // 音量が小さすぎる場合は無視
        //if (db < minimumDb)
        //{
        //    if (logCommand)
        //    {
        //        Debug.Log(
        //            $"[Voice] ignored small input command={command} " +
        //            $"volume={volume:F4} db={db:F1}"
        //        );
        //    }

        //    return;
        //}

        if (logCommand)
        {
            Debug.Log(
                $"[Voice] command={command} " +
                $"volume={volume:F4} db={db:F1}"
            );
        }

        // メニュー用コマンド
        if (IsMenuCommand(command))
        {
            // menuController.ExecuteVoiceCommand(command);
            return;
        }

        switch (command)
        {
            // noneはyaaと同じオブジェクトを処理
            case "none":
                HideOrDestroy(yaaObject, "yaa");
                break;

            case "power":
                HideOrDestroy(powerObject, "power");
                break;

            case "yaa":
                HideOrDestroy(yaaObject, "yaa");
                break;

            case "muscle":
                HideOrDestroy(muscleObject, "muscle");
                break;

            default:
                if (logCommand)
                {
                    Debug.LogWarning(
                        "[Voice] Unknown command: " + command
                    );
                }
                break;
        }
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
            Debug.LogWarning(
                label + " target is not assigned."
            );
            return;
        }

        if (onlyHide)
        {
            // すでに非表示なら重複処理しない
            if (!obj.activeSelf)
            {
                if (logCommand)
                {
                    Debug.Log(
                        label + " is already hidden."
                    );
                }

                return;
            }

            StartCoroutine(HideTemporarily(obj, label));
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

    private IEnumerator HideTemporarily(
        GameObject obj,
        string label
    )
    {
        obj.SetActive(false);

        if (logCommand)
        {
            Debug.Log(
                $"{label} hidden. Show after {hideTime:F1} seconds."
            );
        }

        // ゲーム停止中でも時間を進めたい場合は
        // WaitForSecondsRealtimeを使用
        yield return new WaitForSecondsRealtime(hideTime);

        if (obj != null)
        {
            obj.SetActive(true);

            if (logCommand)
            {
                Debug.Log(label + " shown.");
            }
        }
    }

    private float VolumeToDb(float volume)
    {
        return 20f * Mathf.Log10(
            Mathf.Max(volume, 0.000001f)
        );
    }
}
