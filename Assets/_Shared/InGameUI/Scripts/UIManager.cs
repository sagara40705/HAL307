using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    //== シングルトンパターンの実装 ==//
    // シングルトンのインスタンス
    public static UIManager Instance;

    private ScoreManager _scoreManager;

    //== UI要素の参照 ==//
    [Header("画面パネル")]
    public GameObject gamePanel;   // ゲームUI
    public GameObject resultPanel; // リザルトUI


    [Header("ゲームUI")]
    public FeverGaugeController gauge;   // ゲージ

    [Header("スコアUI")]
    public TextMeshProUGUI playerScoreText;
    public TextMeshProUGUI enemy1ScoreText;
    public TextMeshProUGUI enemy2ScoreText;

    //[Header("リザルトUI")]


    // シングルトンの初期化
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ShowGameScreen();

        _scoreManager = ScoreManager.Instance;
        if (_scoreManager != null)
        {
            _scoreManager.OnScoreChanged += UpdateScoreUI;
        }

        UpdateScoreUI(); // 初期表示
    }
    private void OnDestroy()
    {
        // オブジェクトが破棄されるときはイベントを解除する（メモリリーク防止）
        if (_scoreManager != null)
        {
            _scoreManager.OnScoreChanged -= UpdateScoreUI;
        }

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void UpdateScoreUI()
    {
        if (_scoreManager == null) return;

        if (playerScoreText != null)
            playerScoreText.text = _scoreManager.PlayerScore.ToString();

        if (enemy1ScoreText != null)
            enemy1ScoreText.text = _scoreManager.Enemy1Score.ToString();

        if (enemy2ScoreText != null)
            enemy2ScoreText.text = _scoreManager.Enemy2Score.ToString();
    }

    //== 画面切り替えのメソッド ==//
    public void ShowGameScreen()
    {
        gamePanel.SetActive(true);
    }

    public void ShowResultScreen()
    {
        gamePanel.SetActive(false);
    }
}
