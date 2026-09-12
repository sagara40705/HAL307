#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public sealed class ForceUnityAudioEnabledBuildPreprocessor : IPreprocessBuildWithReport
{
    private const string AudioManagerPath = "ProjectSettings/AudioManager.asset";

    public int callbackOrder => int.MaxValue;

    public void OnPreprocessBuild(BuildReport report)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(AudioManagerPath);
        if (assets == null || assets.Length == 0 || assets[0] == null)
        {
            throw new BuildFailedException(
                $"[UnityAudioBuild] Build stopped. Reason=AudioManager could not be loaded. Path='{AudioManagerPath}'");
        }

        SerializedObject audioManager = new SerializedObject(assets[0]);
        SerializedProperty disableUnityAudio = audioManager.FindProperty("m_DisableAudio");
        if (disableUnityAudio == null)
        {
            throw new BuildFailedException(
                "[UnityAudioBuild] Build stopped. Reason=m_DisableAudio was not found in AudioManager.");
        }

        disableUnityAudio.boolValue = false;
        audioManager.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();

        audioManager.Update();
        if (audioManager.FindProperty("m_DisableAudio").boolValue)
        {
            throw new BuildFailedException(
                "[UnityAudioBuild] Build stopped. Reason=Disable Unity Audio could not be turned off.");
        }

        Debug.Log($"[UnityAudioBuild] Disable Unity Audio is OFF. Target={report.summary.platform}");
    }
}

#endif
