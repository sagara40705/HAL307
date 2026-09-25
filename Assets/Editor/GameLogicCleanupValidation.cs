using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using AudienceRequest;
using Enemy;
using FeverGauge.Sandbox;
using Pose;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

// Unityの実型を使い、ゲームを起動せずに整理対象の契約を検証する。
public static class GameLogicCleanupValidation
{
    private static readonly List<Object> CreatedObjects = new();
    private static int _checks;

    [MenuItem("Tools/Validation/Game Logic Cleanup")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            throw new InvalidOperationException("Run validation in Edit Mode.");
        }

        _checks = 0;
        UnityEngine.Random.State randomState = UnityEngine.Random.state;
        try
        {
            ValidateSceneNames();
            ValidateCoroutines();
            ValidateScores();
            ValidateFever();
            ValidateRequestWeights();
            ValidateRequestCounts();
            Debug.Log($"[GameLogicCleanupValidation] PASS: {_checks} checks.");
        }
        finally
        {
            UnityEngine.Random.state = randomState;
            for (int i = CreatedObjects.Count - 1; i >= 0; i--)
            {
                if (CreatedObjects[i] != null)
                {
                    Object.DestroyImmediate(CreatedObjects[i]);
                }
            }
            CreatedObjects.Clear();
        }
    }

    private static void ValidateSceneNames()
    {
        Check(SceneLookup.NormalizeName(null) == "", "Null scene name");
        Check(SceneLookup.NormalizeName("  ") == "", "Blank scene name");
        Check(SceneLookup.NormalizeName("Title") == "Title", "Short scene name");
        Check(SceneLookup.NormalizeName(" Assets\\Scenes\\Title.UNITY ") == "Title", "Scene asset path");
        Check(!SceneLookup.Find(null).IsValid(), "Null scene lookup");

        GamePhaseSceneSettings settings = CreateAsset<GamePhaseSceneSettings>();
        var empty = new GamePhaseSceneBinding();
        var title = new GamePhaseSceneBinding();
        SetField(title, "phase", GamePhase.Title);
        SetField(title, "sceneName", "Assets/Scenes/Title.unity");
        SetField(settings, "phaseScenes", new List<GamePhaseSceneBinding> { null, empty, title });
        Check(settings.TryGetPhaseBySceneName("Title", out GamePhase phase) && phase == GamePhase.Title,
            "Phase lookup skips empty bindings");
        Check(settings.TryGetSceneName(GamePhase.Title, out string scene) && scene == "Assets/Scenes/Title.unity",
            "Phase lookup preserves asset path");
    }

    private static void ValidateCoroutines()
    {
        bool cleanedUp = false;
        IEnumerator Parent(bool fail)
        {
            try { yield return Child(fail); }
            finally { cleanedUp = true; }
        }
        IEnumerator Child(bool fail)
        {
            yield return "frame";
            if (fail) { throw new InvalidOperationException("Expected validation failure"); }
        }

        IEnumerator normal = CoroutineUtility.Run(Parent(false));
        Check(normal.MoveNext() && (string)normal.Current == "frame", "Nested coroutine yield");
        Check(!normal.MoveNext() && cleanedUp, "Normal coroutine cleanup");

        cleanedUp = false;
        IEnumerator failing = CoroutineUtility.Run(Parent(true));
        failing.MoveNext();
        bool threw = false;
        try { failing.MoveNext(); }
        catch (InvalidOperationException) { threw = true; }
        Check(threw && cleanedUp, "Nested exception releases parent state");

        cleanedUp = false;
        IEnumerator cancelled = CoroutineUtility.Run(Parent(false));
        cancelled.MoveNext();
        ((IDisposable)cancelled).Dispose();
        Check(cleanedUp, "Explicit disposal releases parent state");
        Check(!CoroutineUtility.Run(null).MoveNext(), "Null coroutine");
    }

    private static void ValidateScores()
    {
        ScoreManager scores = CreateComponent<ScoreManager>();
        int notifications = 0;
        scores.OnScoreChanged += () => notifications++;
        for (int i = 1; i <= 4; i++)
        {
            scores.AddEnemyScore(i, (EnemySlotId)i);
            Check(scores.GetEnemyScore((EnemySlotId)i) == i * 10, $"Enemy {i} score");
        }
        Check(scores.Enemy1Score == 10 && scores.Enemy2Score == 20 &&
              scores.Enemy3Score == 30 && scores.Enemy4Score == 40, "Public score properties");
        Check(notifications == 4, "One notification per score change");
        scores.AddEnemyScore(0, EnemySlotId.Enemy1);
        scores.AddEnemyScore(1, (EnemySlotId)99);
        scores.AddEnemyScore(1, "missing");
        Check(notifications == 4, "No notification for unchanged or invalid score");
        scores.AddPlayerScore(2, true);
        Check(scores.PlayerScore == 40, "Fever score multiplier");
        scores.SetPlayerScoreAtLeast(20);
        Check(scores.PlayerScore == 40 && notifications == 5, "Score minimum never decreases score");
        scores.ResetEnemyScore(EnemySlotId.Enemy3);
        scores.ResetEnemyScore(EnemySlotId.Enemy3);
        Check(scores.Enemy3Score == 0 && notifications == 6, "Enemy reset is idempotent");
        scores.ResetAllScores();
        Check(scores.PlayerScore == 0 && scores.Enemy1Score == 0 && scores.Enemy2Score == 0 &&
              scores.Enemy3Score == 0 && scores.Enemy4Score == 0, "Reset every score");
    }

    private static void ValidateFever()
    {
        FeverGaugeMgr gauge = CreateComponent<FeverGaugeMgr>();
        Invoke(gauge, "Initialize");
        int changed = 0;
        int started = 0;
        gauge.OnGaugeChanged += (_, _) => changed++;
        gauge.OnFeverStarted += () => started++;
        gauge.SetGauge(0);
        gauge.AddGauge(-1);
        Check(changed == 0, "No unchanged gauge notification");
        gauge.AddGauge(10);
        Check(gauge.CurrentGauge == 10 && changed == 1, "Gauge increment");
        gauge.AddGauge(int.MaxValue);
        Check(gauge.IsFeverMode && gauge.CurrentGauge == gauge.MaxGauge && changed == 2 && started == 1,
            "Gauge overflow protection and single fever notification");
        gauge.AddGauge(10);
        gauge.SetGauge(0);
        Check(gauge.IsFeverMode && changed == 2 && started == 1, "Fever ignores gauge changes");
    }

    private static void ValidateRequestWeights()
    {
        AudienceRequestLevelDesignSettings settings = CreateAsset<AudienceRequestLevelDesignSettings>();
        var weights = new List<RequestSpawnWeight>
        {
            null,
            Weight(PoseID.Pose_Yaa, 1),
            Weight(PoseID.Pose_Power, 3),
            Weight(PoseID.Pose_Muscle, 100, false),
            Weight(PoseID.Pose_Muscle, 0),
        };
        SetField(settings, "_normalRequestWeights", weights);
        SetField(settings, "_blackRequestWeights", weights);
        var onlyPower = new List<PoseID> { PoseID.Pose_Power };
        Check(settings.GetRandomNormalPoseFromCandidates(onlyPower) == PoseID.Pose_Power, "Normal candidate filter");
        Check(settings.GetRandomBlackPoseFromCandidates(onlyPower) == PoseID.Pose_Power, "Black candidate filter");
        Check(settings.GetRandomNormalPoseFromCandidates(new List<PoseID>()) == PoseID.None, "Empty candidates");
        Check(settings.GetRandomBlackPoseFromCandidates(null) == PoseID.None, "Null candidates");
        Check(settings.GetRandomBlackPoseFromCandidates(new List<PoseID> { PoseID.Pose_Muscle }) == PoseID.None,
            "Disabled and zero weights excluded");

        UnityEngine.Random.InitState(307);
        int yaa = 0;
        int power = 0;
        for (int i = 0; i < 4000; i++)
        {
            PoseID result = settings.GetRandomNormalPose();
            if (result == PoseID.Pose_Yaa) { yaa++; }
            if (result == PoseID.Pose_Power) { power++; }
        }
        Check(yaa + power == 4000 && yaa > 800 && yaa < 1200, "Weighted distribution remains 1:3");
    }

    private static void ValidateRequestCounts()
    {
        AudienceRequests requests = CreateAsset<AudienceRequests>();
        SetField(requests, "_requests", new List<AudienceRequestEntry>
        {
            null,
            new AudienceRequestEntry { poseID = PoseID.Pose_Yaa },
            new AudienceRequestEntry { poseID = PoseID.Pose_Power },
        });
        requests.AddCount(PoseID.Pose_Yaa);
        requests.AddCount(PoseID.Pose_Yaa);
        requests.AddCount(PoseID.Pose_Muscle);
        Check(requests.GetCount(PoseID.Pose_Yaa) == 2 && requests.GetCount(PoseID.Pose_Muscle) == 0,
            "Request lookup skips null and missing entries");
        AudienceRequestSystem system = CreateComponent<AudienceRequestSystem>();
        SetField(system, "_audienceRequests", requests);
        SetField(system, "_currentRequestCount", 2);
        system.ClearTutorialRequestWithoutReward(PoseID.Pose_Yaa, true);
        system.ClearTutorialRequestWithoutReward(PoseID.Pose_Yaa, true);
        Check(system.CurrentRequestCount == 0 && requests.GetCount(PoseID.Pose_Yaa) == 0,
            "Repeated request clear cannot underflow");
        requests.ClearAll();
        Check(requests.Contains(PoseID.Pose_Yaa) && !requests.Contains(PoseID.Pose_Muscle), "Request membership");
    }

    private static RequestSpawnWeight Weight(PoseID pose, int weight, bool enabled = true)
    {
        var result = new RequestSpawnWeight();
        SetField(result, "_poseID", pose);
        SetField(result, "_weight", weight);
        SetField(result, "_isEnabled", enabled);
        return result;
    }

    private static T CreateAsset<T>() where T : ScriptableObject
    {
        T asset = ScriptableObject.CreateInstance<T>();
        CreatedObjects.Add(asset);
        return asset;
    }

    private static T CreateComponent<T>() where T : Component
    {
        var go = new GameObject($"CleanupValidation_{typeof(T).Name}");
        go.hideFlags = HideFlags.HideAndDontSave;
        go.SetActive(false);
        CreatedObjects.Add(go);
        return go.AddComponent<T>();
    }

    private static void SetField(object target, string name, object value)
    {
        FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null) { throw new MissingFieldException(target.GetType().Name, name); }
        field.SetValue(target, value);
    }

    private static void Invoke(object target, string method)
    {
        target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(target, null);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) { throw new InvalidOperationException($"Validation failed: {message}"); }
        _checks++;
    }
}
