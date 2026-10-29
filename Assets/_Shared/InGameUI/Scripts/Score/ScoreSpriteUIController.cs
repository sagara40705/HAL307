using AudienceRequest;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScoreSpriteUIController : MonoBehaviour
{
    [Header("スコア表示")]
    [SerializeField] private ScoreSpriteDisplay playerScoreDisplay;
    [SerializeField] private ScoreSpriteDisplay enemy1ScoreDisplay;
    [SerializeField] private ScoreSpriteDisplay enemy2ScoreDisplay;
    [Header("NPC score frames (separate siblings of the digit displays)")]
    [SerializeField] private RectTransform enemy1ScorePanel;
    [SerializeField] private RectTransform enemy2ScorePanel;

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
    private Enemy.EnemyLineupManager enemyLineup;
    private readonly Dictionary<Animator, ScoreAnimationDefaults> animationDefaults = new();

    private sealed class ScoreAnimationDefaults
    {
        public Vector3 Scale;
        public Image[] Images;
        public Color[] Colors;
    }

    private void Awake()
    {
        CaptureAnimationDefaults(playerScoreAnimator);
        CaptureAnimationDefaults(enemy1ScoreAnimator);
        CaptureAnimationDefaults(enemy2ScoreAnimator);
    }

    private void CaptureAnimationDefaults(Animator animator)
    {
        if (animator == null) { return; }
        Image[] images = animator.GetComponentsInChildren<Image>(true);
        Color[] colors = new Color[images.Length];
        for (int i = 0; i < images.Length; i++) { colors[i] = images[i].color; }
        animationDefaults[animator] = new ScoreAnimationDefaults
        {
            Scale = animator.transform.localScale, Images = images, Colors = colors
        };
    }

    private void ResetScoreAnimation(Animator animator)
    {
        if (animator == null || !animationDefaults.TryGetValue(animator, out ScoreAnimationDefaults defaults)) { return; }
        RestoreScoreVisuals(animator, defaults);
        if (animator.isActiveAndEnabled)
        {
            // 非表示時に残った拡大状態ではなく、制作時のスケールを基準に再設定する。
            animator.Rebind();
            animator.ResetTrigger(scoreUpTriggerName);
            animator.Play("Idle", 0, 0f);
            animator.Update(0f);
        }
        RestoreScoreVisuals(animator, defaults);
    }

    private static void RestoreScoreVisuals(Animator animator, ScoreAnimationDefaults defaults)
    {
        animator.transform.localScale = defaults.Scale;
        for (int i = 0; i < defaults.Images.Length; i++)
        {
            if (defaults.Images[i] != null) { defaults.Images[i].color = defaults.Colors[i]; }
        }
    }

    private bool IsEndlessMode => GameModeManager.Instance != null &&
        GameModeManager.Instance.IsInitialized &&
        GameModeManager.Instance.CurrentGameMode == GameMode.FivePlayer;

    private void Start()
    {
        if (ScoreManager.Instance != null)
        {
            previousPlayerScore = ScoreManager.Instance.PlayerScore;
            ReadEnemyScores(out previousEnemy1Score, out previousEnemy2Score, out _, out _);

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

    private void OnEnable()
    {
        // 既存のWave演出はこのUIの親を非表示にするため、再表示前に更新する。
        if (!IsEndlessMode) { return; }
        ResetScoreAnimation(playerScoreAnimator);
        ResetScoreAnimation(enemy1ScoreAnimator);
        ResetScoreAnimation(enemy2ScoreAnimator);
        if (isScoreInitialized) { UpdateScoreUI(); }
    }

    private void LateUpdate()
    {
        if (!IsEndlessMode || ScoreManager.Instance == null) { return; }
        // 加点がなくても参加・確定状態は変化する。特に、新規参加NPCは0点の場合がある。
        ReadEnemyScores(out int rightScore, out int leftScore, out bool hasRight, out bool hasLeft);
        if (rightScore != previousEnemy1Score || leftScore != previousEnemy2Score ||
            (enemy1ScoreDisplay != null && enemy1ScoreDisplay.gameObject.activeSelf != hasRight) ||
            (enemy2ScoreDisplay != null && enemy2ScoreDisplay.gameObject.activeSelf != hasLeft))
        {
            UpdateScoreUI();
        }
    }

    private void ReadEnemyScores(out int rightScore, out int leftScore, out bool hasRight, out bool hasLeft)
    {
        ScoreManager scores = ScoreManager.Instance;
        if (!IsEndlessMode)
        {
            rightScore = scores.Enemy1Score;
            leftScore = scores.Enemy2Score;
            hasRight = hasLeft = true;
            return;
        }

        if (enemyLineup == null) { enemyLineup = FindFirstObjectByType<Enemy.EnemyLineupManager>(); }
        hasRight = scores.TryGetEndlessGroupScore(enemyLineup, ScoreManager.EndlessScoreGroup.ScreenRight, out rightScore);
        hasLeft = scores.TryGetEndlessGroupScore(enemyLineup, ScoreManager.EndlessScoreGroup.ScreenLeft, out leftScore);
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
        ReadEnemyScores(out int enemy1Score, out int enemy2Score, out bool hasRight, out bool hasLeft);
        bool endless = IsEndlessMode;

        if (endless)
        {
            // Inspectorの接続は変数名と左右が逆で、enemy1ScoreDisplayは画面右を指す。
            if (enemy1ScoreDisplay != null) { enemy1ScoreDisplay.gameObject.SetActive(hasRight); }
            if (enemy2ScoreDisplay != null) { enemy2ScoreDisplay.gameObject.SetActive(hasLeft); }
            if (enemy1ScorePanel != null) { enemy1ScorePanel.gameObject.SetActive(hasRight); }
            if (enemy2ScorePanel != null) { enemy2ScorePanel.gameObject.SetActive(hasLeft); }
            if (playerScore < previousPlayerScore) { ResetScoreAnimation(playerScoreAnimator); }
            if (enemy1Score < previousEnemy1Score) { ResetScoreAnimation(enemy1ScoreAnimator); }
            if (enemy2Score < previousEnemy2Score) { ResetScoreAnimation(enemy2ScoreAnimator); }
        }

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

            if (enemy1Score > previousEnemy1Score && (!endless ||
                (hasRight && enemy1ScoreDisplay != null && enemy1ScoreDisplay.gameObject.activeInHierarchy)))
            {
                PlayScoreAnimation(enemy1ScoreAnimator);
            }

            if (enemy2Score > previousEnemy2Score && (!endless ||
                (hasLeft && enemy2ScoreDisplay != null && enemy2ScoreDisplay.gameObject.activeInHierarchy)))
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
