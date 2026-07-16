using System;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    public int PlayerScore { get; private set; }
    public int Enemy1Score { get; private set; }
    public int Enemy2Score { get; private set; }

    [SerializeField] private int AddScore = 10;

    [Header("フィーバー中のプレイヤー加算倍率")]
    [SerializeField] private int feverScoreMultiplier = 2;

    public event Action OnScoreChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // プレイヤースコア加算
    public void AddPlayerScore(int score, bool isFever = false)
    {
        int multiplier = isFever ? feverScoreMultiplier : 1;

        PlayerScore += score * AddScore * multiplier;

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
            Enemy1Score += score * AddScore;
        }
        else if (slot == Enemy.EnemySlotId.Enemy2)
        {
            Enemy2Score += score * AddScore;
        }

        OnScoreChanged?.Invoke();
    }

    public int GetEnemyScore(Enemy.EnemySlotId slot)
    {
        if (slot == Enemy.EnemySlotId.Enemy1)
        {
            return Enemy1Score;
        }

        return Enemy2Score;
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

        OnScoreChanged?.Invoke();
    }

    public void ResetScoresForTutorial()
    {
        PlayerScore = 0;
        Enemy1Score = 0;
        Enemy2Score = 0;

        OnScoreChanged?.Invoke();
    }


}
