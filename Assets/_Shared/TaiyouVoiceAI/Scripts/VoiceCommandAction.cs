using UnityEngine;

public class VoiceCommandAction : MonoBehaviour
{
    [Header("対象オブジェクト")]
    [SerializeField] private GameObject powerObject;
    [SerializeField] private GameObject yaaObject;
    [SerializeField] private GameObject muscleObject;

    [Header("完全削除ではなく非表示にする")]
    [SerializeField] private bool onlyHide = true;

    [Header("ログを出す")]
    [SerializeField] private bool logCommand = true;

    public void ExecuteCommand(string command)
    {
        if (logCommand)
        {
            Debug.Log("認識コマンド: " + command);
        }

        if (string.IsNullOrEmpty(command))
        {
            return;
        }

        switch (command)
        {
            case "power":
                HideOrDestroy(powerObject, "power");
                break;

            case "yaa":
                HideOrDestroy(yaaObject, "yaa");
                break;

            case "muscle":
                HideOrDestroy(muscleObject, "muscle");
                break;

            case "none":
                // 何もしない
                break;

            default:
                Debug.Log("未対応コマンド: " + command);
                break;
        }
    }

    private void HideOrDestroy(GameObject target, string label)
    {
        if (target == null)
        {
            Debug.LogWarning(label + " 用のオブジェクトが設定されていません");
            return;
        }

        if (onlyHide)
        {
            if (target.activeSelf)
            {
                target.SetActive(false);
                Debug.Log(label + " のオブジェクトを非表示にしました");
            }
        }
        else
        {
            Destroy(target);
            Debug.Log(label + " のオブジェクトを削除しました");
        }
    }
}