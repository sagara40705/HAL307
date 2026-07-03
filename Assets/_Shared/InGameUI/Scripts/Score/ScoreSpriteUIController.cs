using AudienceRequest;
using UnityEngine;

public class ScoreSpriteUIController : MonoBehaviour
{
    [Header("スコア表示")]
    [SerializeField] private ScoreSpriteDisplay playerScoreDisplay;
    [SerializeField] private ScoreSpriteDisplay enemy1ScoreDisplay;
    [SerializeField] private ScoreSpriteDisplay enemy2ScoreDisplay;

    [Header("フィーバー判定")]
    [SerializeField] private FeverGaugeController feverGaugeController;

    private void Start()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged += UpdateScoreUI;
        }

        UpdateScoreUI();
    }

    private void OnDestroy()
    {
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.OnScoreChanged -= UpdateScoreUI;
        }
    }

    private void UpdateScoreUI()
    {
        if (ScoreManager.Instance == null)
        {
            return;
        }

        bool isFever = feverGaugeController != null && feverGaugeController.IsFever;

        if (playerScoreDisplay != null)
        {
            playerScoreDisplay.SetScore(ScoreManager.Instance.PlayerScore, isFever);
        }

        if (enemy1ScoreDisplay != null)
        {
            enemy1ScoreDisplay.SetScore(ScoreManager.Instance.Enemy1Score, false);
        }

        if (enemy2ScoreDisplay != null)
        {
            enemy2ScoreDisplay.SetScore(ScoreManager.Instance.Enemy2Score, false);
        }
    }
}
