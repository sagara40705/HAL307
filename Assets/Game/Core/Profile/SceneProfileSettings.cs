using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "HAL/Profile/Scene Profile Settings")]
public sealed class SceneProfileSettings : ScriptableObject
{
    public const string ResourcesName = "SceneProfileSettings";

    [SerializeField] private bool autoMeasure = true;
    [SerializeField, Min(1f)] private float sampleIntervalSeconds = 10f;
    [SerializeField, Min(0)] private int settleFrames = 1;
    [SerializeField, Min(1)] private int sampleFrames = 8;
    [SerializeField] private List<SceneProfileTarget> targets = new List<SceneProfileTarget>();

    public bool AutoMeasure => autoMeasure;
    public float SampleIntervalSeconds => Mathf.Max(1f, sampleIntervalSeconds);
    public int SettleFrames => Mathf.Max(0, settleFrames);
    public int SampleFrames => Mathf.Max(1, sampleFrames);
    public List<SceneProfileTarget> Targets => targets;

    public static SceneProfileSettings Load()
    {
        return Resources.Load<SceneProfileSettings>(ResourcesName);
    }

    public bool HasEnabledTargets()
    {
        for (int i = 0; i < targets.Count; i++)
        {
            SceneProfileTarget target = targets[i];
            if (target != null && target.IsEnabled)
            {
                return true;
            }
        }

        return false;
    }
}

[Serializable]
public sealed class SceneProfileTarget
{
    [SerializeField] private string sceneName;
    [SerializeField] private string scenePath;
    [SerializeField] private bool isEnabled;

    public string SceneName => sceneName;
    public string ScenePath => scenePath;
    public bool IsEnabled => isEnabled;

    public SceneProfileTarget(string sceneName, string scenePath, bool isEnabled)
    {
        this.sceneName = sceneName;
        this.scenePath = scenePath;
        this.isEnabled = isEnabled;
    }

    public void SetEnabled(bool value)
    {
        isEnabled = value;
    }

    public void UpdateScene(string newSceneName, string newScenePath)
    {
        sceneName = newSceneName;
        scenePath = newScenePath;
    }
}

