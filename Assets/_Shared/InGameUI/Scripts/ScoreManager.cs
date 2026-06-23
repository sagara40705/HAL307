using System;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    // 外部からは読み取り専用のプロパティ
    public int PlayerScore { get; private set; }
    public int Enemy1Score { get; private set; }
    public int Enemy2Score { get; private set; }

    //
    [SerializeField] private int AddScore = 10; // スコア加算量

    // スコアが更新されたことをUI等に通知するイベント
    public event Action OnScoreChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // シーンを跨いでもスコアを保持したい場合は以下を有効
            // DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // プレイヤースコア加算
    public void AddPlayerScore(int score)
    {
        PlayerScore += score * AddScore;
        OnScoreChanged?.Invoke(); // UI等に通知
    }

    // エネミーのスコア加算（名前で自動判定）
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

        OnScoreChanged?.Invoke(); // UI等に通知
    }
}
