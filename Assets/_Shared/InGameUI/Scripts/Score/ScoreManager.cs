using System;
using Enemy;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScoreManager : MonoBehaviour
{
    public enum EndlessScoreGroup
    {
        ScreenLeft,
        ScreenRight
    }
    [Header("スコア初期化")]
    [SerializeField]
    private string _inGameSceneName = "InGameUI";

    private const int DefaultScoreUnit = 10;
    private const int DefaultFeverScoreMultiplier = 2;

    public static ScoreManager Instance { get; private set; }

    public int PlayerScore { get; private set; }
    private readonly int[] _enemyScores = new int[(int)EnemySlotId.Enemy4];

    public int Enemy1Score => GetEnemyScore(EnemySlotId.Enemy1);
    public int Enemy2Score => GetEnemyScore(EnemySlotId.Enemy2);
    public int Enemy3Score => GetEnemyScore(EnemySlotId.Enemy3);
    public int Enemy4Score => GetEnemyScore(EnemySlotId.Enemy4);

    [SerializeField] private ScoreSettings scoreSettings;

    public event Action OnScoreChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

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

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (!SceneLookup.Matches(scene, _inGameSceneName))
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
        int delta = score * ScoreUnit * multiplier;
        if (delta == 0)
        {
            return;
        }

        PlayerScore += delta;

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
            return;
        }

        AddEnemyScore(score, slot);
    }

    public void AddEnemyScore(int score, Enemy.EnemySlotId slot)
    {
        SetEnemyScore(slot, GetEnemyScore(slot) + score * ScoreUnit);
    }

    public int GetEnemyScore(Enemy.EnemySlotId slot)
    {
        int index = (int)slot - 1;
        return index >= 0 && index < _enemyScores.Length ? _enemyScores[index] : 0;
    }

    // Returns group presence separately from its score, so an occupied group can display zero.
    // Individual Slot scores remain the source of truth for both awards and replacement selection.
    public bool TryGetEndlessGroupScore(EnemyLineupManager lineup, EndlessScoreGroup group, out int score)
    {
        score = 0;
        GameModeManager mode = GameModeManager.Instance;
        if (mode == null || !mode.IsInitialized || mode.CurrentGameMode != GameMode.FivePlayer || lineup == null)
        {
            return false;
        }

        EnemySlotId first;
        EnemySlotId second;
        switch (group)
        {
            // Camera faces Y=180: positive world X is screen left.
            case EndlessScoreGroup.ScreenLeft:
                first = EnemySlotId.Enemy2;
                second = EnemySlotId.Enemy3;
                break;
            case EndlessScoreGroup.ScreenRight:
                first = EnemySlotId.Enemy1;
                second = EnemySlotId.Enemy4;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(group));
        }

        bool hasFirst = IsCommittedParticipant(lineup, first);
        bool hasSecond = IsCommittedParticipant(lineup, second);
        long total = (hasFirst ? (long)GetEnemyScore(first) : 0L) +
                     (hasSecond ? (long)GetEnemyScore(second) : 0L);
        score = (int)Math.Min(total, int.MaxValue);
        return hasFirst || hasSecond;
    }

    private static bool IsCommittedParticipant(EnemyLineupManager lineup, EnemySlotId slot)
    {
        if ((int)slot > lineup.MaxEnemyCount) { return false; }
        Enemy.Enemy enemy = lineup.GetEnemy(slot);
        if (enemy == null || !enemy.ParticipatesInRound || !enemy.gameObject.activeInHierarchy)
        {
            return false;
        }

        // Exclude the selected outgoing actor even before its presentation flag changes.
        return !lineup.HasPendingReplacement ||
               (enemy != lineup.PendingOldEnemy && enemy != lineup.PendingNewEnemy);
    }

    public Enemy.EnemySlotId GetLowestEnemySlot()
    {
        return Enemy1Score <= Enemy2Score ? Enemy.EnemySlotId.Enemy1 : Enemy.EnemySlotId.Enemy2;
    }

    public void ResetEnemyScore(Enemy.EnemySlotId slot)
    {
        SetEnemyScore(slot, 0);
    }

    private void SetEnemyScore(EnemySlotId slot, int score)
    {
        int index = (int)slot - 1;
        if (index < 0 || index >= _enemyScores.Length)
        {
            Debug.LogWarning($"[ScoreManager] Score update skipped. Reason=Unknown enemy slot. Slot={slot}");
            return;
        }

        if (_enemyScores[index] == score)
        {
            return;
        }

        _enemyScores[index] = score;
        OnScoreChanged?.Invoke();
    }

    public void ResetAllScores()
    {
        PlayerScore = 0;
        Array.Clear(_enemyScores, 0, _enemyScores.Length);

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
