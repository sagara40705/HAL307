using UnityEngine;

public class ResultScoreUIController : MonoBehaviour
{
    [Header("リザルトスコア表示")]
    [SerializeField] private ScoreSpriteDisplay playerScoreDisplay;
    [SerializeField] private ScoreSpriteDisplay enemy1ScoreDisplay;
    [SerializeField] private ScoreSpriteDisplay enemy2ScoreDisplay;

    // リザルトスコア表示用
    private void Start()
    {
        if (ScoreManager.Instance == null)
        {
            Debug.LogWarning("[ResultScoreUIController] ScoreManager.Instance was not found.");
            return;
        }

        playerScoreDisplay?.SetScore(ScoreManager.Instance.PlayerScore, false);
        enemy1ScoreDisplay?.SetScore(ScoreManager.Instance.Enemy1Score, false);
        enemy2ScoreDisplay?.SetScore(ScoreManager.Instance.Enemy2Score, false);
    }
}
