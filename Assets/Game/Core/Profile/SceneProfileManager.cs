using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SceneProfileSampleState
{
    Measured,
    NotLoaded,
    NoRootObjects,
    Error,
}

[Serializable]
public sealed class SceneProfileReport
{
    public string measuredAt;
    public int settleFrames;
    public int sampleFrames;
    public List<SceneProfileSample> samples = new List<SceneProfileSample>();
}

[Serializable]
public sealed class SceneProfileSample
{
    public string sceneName;
    public string scenePath;
    public SceneProfileSampleState state;
    public float baselineMs;
    public float disabledMs;
    public float estimatedSceneMs;
    public int rootObjectCount;
    public string note;
}

public sealed class SceneProfileManager : AppManagerBase<SceneProfileManager>
{
    private const string LastResultPrefsKey = "HAL.SceneProfile.LastResultJson.v1";

    private SceneProfileSettings _settings;
    private bool _isMeasuring;
    private bool _measureRequested;

    public static SceneProfileReport LastReport { get; private set; }
    public bool IsMeasuring => _isMeasuring;

    protected override IEnumerator OnInitialize()
    {
        _settings = SceneProfileSettings.Load();

        if (_settings == null)
        {
            Debug.LogWarning("[SceneProfileManager] SceneProfileSettings was not found. Open Tools/Profile/Settings to create it.");
        }

        StartCoroutine(ProfileLoop());
        yield break;
    }

    public void RequestMeasureOnce()
    {
        _measureRequested = true;
    }

    public static SceneProfileReport LoadLastReportFromPrefs()
    {
        string json = PlayerPrefs.GetString(LastResultPrefsKey, string.Empty);
        if (string.IsNullOrEmpty(json))
        {
            return null;
        }

        try
        {
            return JsonUtility.FromJson<SceneProfileReport>(json);
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SceneProfileManager] Failed to load last report. {ex.Message}");
            return null;
        }
    }

    public static void ClearLastReport()
    {
        LastReport = null;
        PlayerPrefs.DeleteKey(LastResultPrefsKey);
        PlayerPrefs.Save();
    }

    private IEnumerator ProfileLoop()
    {
        while (true)
        {
            _settings = SceneProfileSettings.Load();

            bool requestedMeasure = _measureRequested;
            _measureRequested = false;

            bool shouldMeasure = requestedMeasure || (_settings != null && _settings.AutoMeasure);
            if (shouldMeasure && _settings != null && _settings.HasEnabledTargets())
            {
                yield return MeasureOnce(_settings);
            }

            float waitSeconds = _settings != null ? _settings.SampleIntervalSeconds : 10f;
            float elapsed = 0f;
            while (elapsed < waitSeconds && !_measureRequested)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }
    }

    private IEnumerator MeasureOnce(SceneProfileSettings settings)
    {
        if (_isMeasuring)
        {
            yield break;
        }

        _isMeasuring = true;

        SceneProfileReport report = new SceneProfileReport
        {
            measuredAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            settleFrames = settings.SettleFrames,
            sampleFrames = settings.SampleFrames,
        };

        List<SceneProfileTarget> targets = new List<SceneProfileTarget>(settings.Targets);
        for (int i = 0; i < targets.Count; i++)
        {
            SceneProfileTarget target = targets[i];
            if (target == null || !target.IsEnabled)
            {
                continue;
            }

            SceneProfileSample sample = new SceneProfileSample
            {
                sceneName = target.SceneName,
                scenePath = target.ScenePath,
            };

            if (!TryGetLoadedScene(target, out Scene scene))
            {
                sample.state = SceneProfileSampleState.NotLoaded;
                sample.note = "Scene is selected but not loaded.";
                report.samples.Add(sample);
                continue;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            sample.rootObjectCount = roots.Length;

            if (roots.Length == 0)
            {
                sample.state = SceneProfileSampleState.NoRootObjects;
                sample.note = "Scene has no root GameObjects.";
                report.samples.Add(sample);
                continue;
            }

            float baselineMs = 0f;
            yield return CaptureAverageFrameMs(settings.SampleFrames, value => baselineMs = value);

            RootActiveState[] rootStates = DisableSceneRoots(roots);
            yield return WaitFrames(settings.SettleFrames);

            float disabledMs = 0f;
            yield return CaptureAverageFrameMs(settings.SampleFrames, value => disabledMs = value);

            RestoreSceneRoots(rootStates);
            yield return WaitFrames(settings.SettleFrames);

            sample.state = SceneProfileSampleState.Measured;
            sample.baselineMs = baselineMs;
            sample.disabledMs = disabledMs;
            sample.estimatedSceneMs = baselineMs - disabledMs;
            sample.note = "Estimated from baseline frame time minus frame time while scene roots are disabled.";
            report.samples.Add(sample);
        }

        LastReport = report;
        SaveReport(report);
        _isMeasuring = false;
    }

    private static bool TryGetLoadedScene(SceneProfileTarget target, out Scene scene)
    {
        scene = default;

        if (!string.IsNullOrEmpty(target.ScenePath))
        {
            scene = SceneManager.GetSceneByPath(target.ScenePath);
            if (scene.IsValid() && scene.isLoaded)
            {
                return true;
            }
        }

        if (!string.IsNullOrEmpty(target.SceneName))
        {
            scene = SceneManager.GetSceneByName(target.SceneName);
            return scene.IsValid() && scene.isLoaded;
        }

        return false;
    }

    private static IEnumerator CaptureAverageFrameMs(int frameCount, Action<float> setResult)
    {
        int count = Mathf.Max(1, frameCount);
        double start = Time.realtimeSinceStartupAsDouble;

        for (int i = 0; i < count; i++)
        {
            yield return null;
        }

        double elapsed = Time.realtimeSinceStartupAsDouble - start;
        setResult((float)(elapsed * 1000.0 / count));
    }

    private static IEnumerator WaitFrames(int frameCount)
    {
        for (int i = 0; i < frameCount; i++)
        {
            yield return null;
        }
    }

    private static RootActiveState[] DisableSceneRoots(GameObject[] roots)
    {
        RootActiveState[] states = new RootActiveState[roots.Length];

        for (int i = 0; i < roots.Length; i++)
        {
            GameObject root = roots[i];
            states[i] = new RootActiveState(root, root != null && root.activeSelf);

            if (root != null && root.activeSelf)
            {
                root.SetActive(false);
            }
        }

        return states;
    }

    private static void RestoreSceneRoots(RootActiveState[] states)
    {
        for (int i = 0; i < states.Length; i++)
        {
            RootActiveState state = states[i];
            if (state.Root != null)
            {
                state.Root.SetActive(state.WasActive);
            }
        }
    }

    private static void SaveReport(SceneProfileReport report)
    {
        string json = JsonUtility.ToJson(report);
        PlayerPrefs.SetString(LastResultPrefsKey, json);
        PlayerPrefs.Save();
    }

    private readonly struct RootActiveState
    {
        public readonly GameObject Root;
        public readonly bool WasActive;

        public RootActiveState(GameObject root, bool wasActive)
        {
            Root = root;
            WasActive = wasActive;
        }
    }
}
