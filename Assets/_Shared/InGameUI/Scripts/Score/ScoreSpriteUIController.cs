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

    [Header("スコアUI出方調整")]
    [SerializeField] private AdjustableUiPresenter scoreUiPresenter;
    [SerializeField] private bool showScoreUiOnStart = true;
    [SerializeField] private bool playScoreUiOnScoreChanged = false;

    [Header("スコア全体の増加アニメーション")]
    [SerializeField] private Animator playerScoreAnimator;
    [SerializeField] private Animator enemy1ScoreAnimator;
    [SerializeField] private Animator enemy2ScoreAnimator;

    [SerializeField] private string scoreUpTriggerName = "ScoreUp";

    private int previousPlayerScore;
    private int previousEnemy1Score;
    private int previousEnemy2Score;
    private bool isScoreInitialized;

    private void Start()
    {
        if (ScoreManager.Instance != null)
        {
            previousPlayerScore = ScoreManager.Instance.PlayerScore;
            previousEnemy1Score = ScoreManager.Instance.Enemy1Score;
            previousEnemy2Score = ScoreManager.Instance.Enemy2Score;

            isScoreInitialized = true;

            ScoreManager.Instance.OnScoreChanged += UpdateScoreUI;
        }

        if (showScoreUiOnStart && scoreUiPresenter != null)
        {
            scoreUiPresenter.CaptureCurrentAsBase();
            scoreUiPresenter.Show();
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

        int playerScore = ScoreManager.Instance.PlayerScore;
        int enemy1Score = ScoreManager.Instance.Enemy1Score;
        int enemy2Score = ScoreManager.Instance.Enemy2Score;

        bool isFever =
            feverGaugeController != null &&
            feverGaugeController.IsFever;

        playerScoreDisplay?.SetScore(playerScore, isFever);
        enemy1ScoreDisplay?.SetScore(enemy1Score, false);
        enemy2ScoreDisplay?.SetScore(enemy2Score, false);

        if (isScoreInitialized)
        {
            if (playerScore > previousPlayerScore)
            {
                PlayScoreAnimation(playerScoreAnimator);
            }

            if (enemy1Score > previousEnemy1Score)
            {
                PlayScoreAnimation(enemy1ScoreAnimator);
            }

            if (enemy2Score > previousEnemy2Score)
            {
                PlayScoreAnimation(enemy2ScoreAnimator);
            }
        }

        if (playScoreUiOnScoreChanged && scoreUiPresenter != null)
        {
            scoreUiPresenter.CaptureCurrentAsBase();
            scoreUiPresenter.Show();
        }

        previousPlayerScore = playerScore;
        previousEnemy1Score = enemy1Score;
        previousEnemy2Score = enemy2Score;
        isScoreInitialized = true;
    }

    private void PlayScoreAnimation(Animator animator)
    {
        if (animator == null)
        {
            return;
        }

        animator.ResetTrigger(scoreUpTriggerName);
        animator.SetTrigger(scoreUpTriggerName);
    }

}
