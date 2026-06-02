using UnityEngine;

public class UIManager : MonoBehaviour
{
    //== シングルトンパターンの実装 ==//
    // シングルトンのインスタンス
    public static UIManager Instance;

    //== UI要素の参照 ==//
    [Header("画面パネル")]
    public GameObject gamePanel;   // ゲームUI
    public GameObject resultPanel; // リザルトUI


    //各UI要素の参照を追加
    //[Header("タイトルUI")]

    [Header("ゲームUI")]
    //public BillboardSpawner spawner; // 吹き出し
    public GaugeController  gauge;   // ゲージ

    //[Header("リザルトUI")]


    // シングルトンの初期化
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ShowGameScreen();
    }

    //== 画面切り替えのメソッド ==//

    public void ShowGameScreen()
    {
        gamePanel.SetActive(true);
        resultPanel.SetActive(false);

        //    if (spawner != null)
        //    {
        //        spawner.StartSpawning();
        //    }
    }

    public void ShowResultScreen()
    {
        gamePanel.SetActive(false);
        resultPanel.SetActive(true);

        //if (spawner != null)
        //{
        //    spawner.StopSpawning();
        //}
    }

    public void ReportHeartChanged(int count)
    {
        if(gauge != null)
        gauge.AddPoints(count);
    }

    public void OnGaugeFull()
    {
        gauge.ResetGauge();
    }

    public void ReturnToTitle()
    {
        SceneController.Instance.GoToTitle();
    }


}
