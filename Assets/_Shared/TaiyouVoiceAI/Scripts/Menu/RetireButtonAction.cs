using UnityEngine;
using UnityEngine.Events;

public class RetireButtonAction : MonoBehaviour
{
    [Header("Retire Event")]
    [SerializeField] private UnityEvent onRetaire;

    public void OnClickRetire()
    {
        Debug.Log("リタイアボタンが押されました");

        // メニューを止めていた時間を戻す
        Time.timeScale = 1f;


        // デバックでゲーム終了する処理
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif


        // あとでタイトルに戻る処理を追加する

    }
}
