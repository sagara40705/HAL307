#if UNITY_EDITOR

using System;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
#if UNITY_6000_0_OR_NEWER
using UnityEditor.Build.Profile;
#endif
using UnityEngine;

public static class InputHandlingSetup
{
    public enum Mode
    {
        OldInputManager = 0,
        NewInputSystem = 1,
        Both = 2,
    }

    public readonly struct Result
    {
        public readonly bool HasDifference;
        public readonly bool Applied;
        public readonly bool RequiresEditorRestart;

        public Result(bool hasDifference, bool applied, bool requiresEditorRestart)
        {
            HasDifference = hasDifference;
            Applied = applied;
            RequiresEditorRestart = requiresEditorRestart;
        }
    }

    private const string ActiveInputHandlerPropertyName = "activeInputHandler";

    /// <summary>
    /// dryRun = true なら検証のみ
    /// dryRun = false なら必要時に適用
    /// </summary>
    public static Result ValidateOrApply(Mode desiredMode, bool dryRun, StringBuilder report)
    {
        if (report == null)
        {
            report = new StringBuilder();
        }

        SerializedProperty property = GetActiveInputHandlerProperty();
        if (property == null)
        {
            report.AppendLine("[InputHandling] NG: activeInputHandler を取得できませんでした。");
            return new Result(hasDifference: true, applied: false, requiresEditorRestart: false);
        }

        int currentValue = property.intValue;
        int desiredValue = (int)desiredMode;

        if (currentValue == desiredValue)
        {
            report.AppendLine($"[InputHandling] OK: {ToLabel(currentValue)}");
            return new Result(hasDifference: false, applied: false, requiresEditorRestart: false);
        }

        if (dryRun)
        {
            report.AppendLine(
                $"[InputHandling] DIFF: current={ToLabel(currentValue)}, desired={ToLabel(desiredValue)}");
            return new Result(hasDifference: true, applied: false, requiresEditorRestart: false);
        }

        property.intValue = desiredValue;
        property.serializedObject.ApplyModifiedProperties();

        report.AppendLine(
            $"[InputHandling] APPLIED: {ToLabel(currentValue)} -> {ToLabel(desiredValue)}");

        return new Result(hasDifference: true, applied: true, requiresEditorRestart: true);
    }

    public static Mode? GetCurrentMode()
    {
        SerializedProperty property = GetActiveInputHandlerProperty();
        if (property == null)
        {
            return null;
        }

        if (!Enum.IsDefined(typeof(Mode), property.intValue))
        {
            Debug.LogWarning($"[InputHandling] Unknown activeInputHandler value: {property.intValue}");
            return null;
        }

        return (Mode)property.intValue;
    }

    private static string ToLabel(int value)
    {
        return value switch
        {
            0 => "Old Input Manager",
            1 => "New Input System",
            2 => "Both",
            _ => $"Unknown({value})"
        };
    }

    private static SerializedProperty GetActiveInputHandlerProperty()
    {
#if UNITY_6000_0_OR_NEWER
        Type buildProfileType = typeof(BuildProfile);

        FieldInfo globalPlayerSettingsField =
            buildProfileType.GetField("s_GlobalPlayerSettings", BindingFlags.Static | BindingFlags.NonPublic);

        if (globalPlayerSettingsField == null)
        {
            Debug.LogError("[InputHandling] BuildProfile.s_GlobalPlayerSettings が見つかりません。");
            return null;
        }

        PlayerSettings playerSettings = globalPlayerSettingsField.GetValue(null) as PlayerSettings;
        if (playerSettings == null)
        {
            Debug.LogError("[InputHandling] Global PlayerSettings を取得できません。");
            return null;
        }

        BuildProfile activeBuildProfile = BuildProfile.GetActiveBuildProfile();
        if (activeBuildProfile != null)
        {
            FieldInfo playerSettingsOverrideField =
                buildProfileType.GetField("m_PlayerSettings", BindingFlags.Instance | BindingFlags.NonPublic);

            if (playerSettingsOverrideField == null)
            {
                Debug.LogError("[InputHandling] BuildProfile.m_PlayerSettings が見つかりません。");
                return null;
            }

            PlayerSettings playerSettingsOverride =
                playerSettingsOverrideField.GetValue(activeBuildProfile) as PlayerSettings;

            if (playerSettingsOverride != null)
            {
                playerSettings = playerSettingsOverride;
            }
        }

        SerializedObject serializedObject = new SerializedObject(playerSettings);
        return serializedObject.FindProperty(ActiveInputHandlerPropertyName);
#else
        PlayerSettings playerSettings = Resources.FindObjectsOfTypeAll<PlayerSettings>().FirstOrDefault();
        if (playerSettings == null)
        {
            Debug.LogError("[InputHandling] PlayerSettings を取得できません。");
            return null;
        }

        SerializedObject serializedObject = new SerializedObject(playerSettings);
        return serializedObject.FindProperty(ActiveInputHandlerPropertyName);
#endif
    }
}

#endif
