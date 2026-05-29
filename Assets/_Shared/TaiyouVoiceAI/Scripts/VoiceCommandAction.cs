using UnityEngine;

public class VoiceCommandAction : MonoBehaviour
{
    // 音声コマンドごとの対象オブジェクト
    [Header("対象オブジェクト")]
    [SerializeField] private GameObject powerObject;
    [SerializeField] private GameObject yaaObject;
    [SerializeField] private GameObject muscleObject;

    // 音量設定
    [Header("音量設定")]
    [SerializeField] private float bigVoiceThreshold = 0.05f;

    // 設定
    // onlyHide = true なら非表示
    // onlyHide = false なら削除
    [Header("設定")]
    [SerializeField] private bool onlyHide = true;
    [SerializeField] private bool logCommand = true;

    public void ExecuteCommand(string command, float volume)
    {
        // コマンドが空なら何もしない
        if (string.IsNullOrEmpty(command))
        {
            return;
        }

        // デバック表示
        if (logCommand)
        {
            Debug.Log("[Action] command = " + command + " / volume = " + volume);
        }


        // 音量をデシベルに変換
        float db = VolumeToDb(volume);

        Debug.Log("dB = " + db);

        // -20dB未満は無視
        if (db < -20f)
        {
            Debug.Log("声が小さいので無視");
            return;
        }

        // 音声コマンドごとの処理
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

    /// オブジェクトを非表示または削除
    private void HideOrDestroy(GameObject obj, string label)
    {
        // オブジェクト未設定チェック
        if (obj == null)
        {
            Debug.LogWarning(label + " の対象オブジェクトが設定されていません");
            return;
        }

        // 非表示モード
        if (onlyHide)
        {
            obj.SetActive(false);
            Debug.Log(label + " → 非表示");
        }
        // 削除モード
        else
        {
            Destroy(obj);
            Debug.Log(label + " → 削除");
        }
    }

    /// 音量(0～1)をデシベルへ変換
    private float VolumeToDb(float volume)
    {
        return 20f * Mathf.Log10(Mathf.Max(volume, 0.000001f));
    }
}
