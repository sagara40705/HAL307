using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScoreManager : MonoBehaviour
{
    [Header("スコア初期化")]
    [SerializeField]
    private string _inGameSceneName = "InGameUI";

    private const int DefaultScoreUnit = 10;
    private const int DefaultFeverScoreMultiplier = 2;

    public static ScoreManager Instance { get; private set; }

    public int PlayerScore { get; private set; }
    public int Enemy1Score { get; private set; }
    public int Enemy2Score { get; private set; }
    public int Enemy3Score { get; private set; }
    public int Enemy4Score { get; private set; }

    [SerializeField] private ScoreSettings scoreSettings;

    public event Action OnScoreChanged;

    // 
    private void Awake()
    {
        //  
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        //  
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;

        if (scoreSettings == null)
        {
            Debug.LogWarning(
                "[ScoreManager] ScoreSettings is not assigned. " +
                "Default score values are used."
            );
        }
    }

    private void OnSceneLoaded(Scene scene,LoadSceneMode loadSceneMode)
    {
        if (scene.name != _inGameSceneName)
        {
            return;
        }

        ResetAllScores();
    }
    private void OnDestroy()
    {
        if (Instance != this)
        {
            return;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
        Instance = null;
    }

    // プレイヤースコア加算
    public void AddPlayerScore(int score, bool isFever = false)
    {
        int multiplier = isFever ? FeverScoreMultiplier : 1;

        PlayerScore += score * ScoreUnit * multiplier;

        OnScoreChanged?.Invoke();
    }

    public void SetPlayerScoreAtLeast(int score)
    {
        if (PlayerScore >= score)
        {
            return;
        }

        PlayerScore = score;
        OnScoreChanged?.Invoke();
    }

    // エネミーのスコア加算
    public void AddEnemyScore(int score, string enemyName)
    {
        if (!Enum.TryParse(enemyName, out Enemy.EnemySlotId slot))
        {
            Debug.LogWarning($"[ScoreManager] AddEnemyScore skipped. Reason=Unknown enemyName. EnemyName={enemyName}");
            OnScoreChanged?.Invoke();
            return;
        }

        AddEnemyScore(score, slot);
    }

    public void AddEnemyScore(int score, Enemy.EnemySlotId slot)
    {
        if (slot == Enemy.EnemySlotId.Enemy1)
        {
            Enemy1Score += score * ScoreUnit;
        }
        else if (slot == Enemy.EnemySlotId.Enemy2)
        {
            Enemy2Score += score * ScoreUnit;
        }
        else if (slot == Enemy.EnemySlotId.Enemy3)
        {
            Enemy3Score += score * ScoreUnit;
        }
        else if (slot == Enemy.EnemySlotId.Enemy4)
        {
            Enemy4Score += score * ScoreUnit;
        }

        OnScoreChanged?.Invoke();
    }

    public int GetEnemyScore(Enemy.EnemySlotId slot)
    {
        if (slot == Enemy.EnemySlotId.Enemy1)
        {
            return Enemy1Score;
        }

        if (slot == Enemy.EnemySlotId.Enemy2)
        {
            return Enemy2Score;
        }

        if (slot == Enemy.EnemySlotId.Enemy3)
        {
            return Enemy3Score;
        }

        return slot == Enemy.EnemySlotId.Enemy4 ? Enemy4Score : 0;
    }

    public Enemy.EnemySlotId GetLowestEnemySlot()
    {
        return Enemy1Score <= Enemy2Score ? Enemy.EnemySlotId.Enemy1 : Enemy.EnemySlotId.Enemy2;
    }

    public void ResetEnemyScore(Enemy.EnemySlotId slot)
    {
        if (slot == Enemy.EnemySlotId.Enemy1)
        {
            Enemy1Score = 0;
        }
        else if (slot == Enemy.EnemySlotId.Enemy2)
        {
            Enemy2Score = 0;
        }
        else if (slot == Enemy.EnemySlotId.Enemy3)
        {
            Enemy3Score = 0;
        }
        else if (slot == Enemy.EnemySlotId.Enemy4)
        {
            Enemy4Score = 0;
        }

        OnScoreChanged?.Invoke();
    }

    public void ResetAllScores()
    {
        PlayerScore = 0;
        Enemy1Score = 0;
        Enemy2Score = 0;
        Enemy3Score = 0;
        Enemy4Score = 0;

        OnScoreChanged?.Invoke();

    }

    public void ResetScoresForTutorial()
    {
        ResetAllScores();
    }

    private int ScoreUnit => scoreSettings != null
        ? scoreSettings.ScoreUnit
        : DefaultScoreUnit;

    private int FeverScoreMultiplier => scoreSettings != null
        ? scoreSettings.FeverScoreMultiplier
        : DefaultFeverScoreMultiplier;


}
