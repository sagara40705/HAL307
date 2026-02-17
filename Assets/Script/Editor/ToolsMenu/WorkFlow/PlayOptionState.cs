using System;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayOptionState", menuName = "Scriptable Objects/PlayOptionState", order = 1)]
public class PlayOptionState : ScriptableObject
{
    // Workflow_PlayOption メニュー用の ON/OFF 状態。
    [Serializable]
    public struct State
    {
        public string key; 
        public bool flag;
    }

    // アセットとして保持するためのシリアライズ済みリスト。
    public State[] states = Array.Empty<State>();

    // キーが存在する場合は true を返し、現在の値を返す。
    public bool TryGetFlag(string key, out bool value)
    {
        value = false;
        if (states == null) return false;

        for (int i = 0; i < states.Length; i++)
        {
            if (!string.Equals(states[i].key, key, StringComparison.OrdinalIgnoreCase))    continue;

            value = states[i].flag;
            return true;
        }

        return false;
    }

    // 簡易版: キーが存在してかつ true の場合のみ true を返す。
    public bool GetFlag(string key)
    {
        return TryGetFlag(key, out var value) && value;
    }

    // 簡易版: キーが存在する場合のみトグルし、新しい値を返す。
    public bool ToggleState(string key)
    {
        return TryToggleState(key, out var value) && value;
    }

    // キーが存在する場合はトグルし、新しい値を返す。
    public bool TryToggleState(string key, out bool value)
    {
        value = false;
        if (states == null) return false;

        for (int i = 0; i < states.Length; i++)
        {
            if (!string.Equals(states[i].key, key, StringComparison.OrdinalIgnoreCase))    continue;

            value = !states[i].flag;
            states[i].flag = value;
            return true;
        }

        return false;
    }

}
