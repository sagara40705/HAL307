using UnityEngine;

public class VoiceCommandAction : MonoBehaviour
{
    [Header("対象オブジェクト")]
    [SerializeField] private GameObject powerObject;
    [SerializeField] private GameObject yaaObject;
    [SerializeField] private GameObject muscleObject;

    [Header("音量設定")]
    [SerializeField] private float bigVoiceThreshold = 0.05f;

    [Header("設定")]
    [SerializeField] private bool onlyHide = true;
    [SerializeField] private bool logCommand = true;

    public void ExecuteCommand(string command, float volume)
    {
        if (string.IsNullOrEmpty(command))
        {
            return;
        }

        if (logCommand)
        {
            Debug.Log("[Action] command = " + command + " / volume = " + volume);
        }

        float db = VolumeToDb(volume);

        Debug.Log("dB = " + db);

        // -20dB未満は無視
        if (db < -20f)
        {
            Debug.Log("声が小さいので無視");
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
            obj.SetActive(false);
            Debug.Log(label + " → 非表示");
        }
        else
        {
            Destroy(obj);
            Debug.Log(label + " → 削除");
        }
    }

    private float VolumeToDb(float volume)
    {
        return 20f * Mathf.Log10(Mathf.Max(volume, 0.000001f));
    }
}
