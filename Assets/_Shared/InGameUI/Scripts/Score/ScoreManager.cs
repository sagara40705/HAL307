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

    // エネミーのスコア加算
    public void AddEnemyScore(int score, string enemyName)
    {
        if (enemyName == "Enemy1")
        {
            Enemy1Score += score * AddScore;
        }
        else if (enemyName == "Enemy2")
        {
            Enemy2Score += score * AddScore;
        }

        OnScoreChanged?.Invoke();
    }
}
