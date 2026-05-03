using UnityEngine;

public class VoiceCommandAction : MonoBehaviour
{
    [Header("対象オブジェクト")]
    [SerializeField] private GameObject powerObject;
    [SerializeField] private GameObject yaaObject;
    [SerializeField] private GameObject muscleObject;

    [Header("設定")]
    [SerializeField] private bool onlyHide = true;
    [SerializeField] private bool logCommand = true;

    public void ExecuteCommand(string command)
    {
        if (string.IsNullOrEmpty(command))
        {
            return;
        }

        if (logCommand)
        {
            Debug.Log("[Action] command = " + command);
        }

        switch (command)
        {
            case "power":
                HandleObject(powerObject, "power");
                break;

            case "yaa":
                HandleObject(yaaObject, "yaa");
                break;

            case "muscle":
                HandleObject(muscleObject, "muscle");
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