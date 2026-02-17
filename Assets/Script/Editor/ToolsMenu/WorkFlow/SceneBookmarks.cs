using System;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "SceneBookmarks", menuName = "Scriptable Objects/SceneBookmarks", order = 0)]
public class SceneBookmarks : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public string key;       // キー
        public SceneAsset scene; // 参照するシーン
    }

    public Entry[] entries = Array.Empty<Entry>();


    public string TryGetPath(string key)
    {
        if (entries == null) return null;

        for (int i = 0; i < entries.Length; i++)
        {
            if (!string.Equals(entries[i].key, key, StringComparison.OrdinalIgnoreCase))    continue;

            var s = entries[i].scene;
            if (s == null) return null;

            var path = AssetDatabase.GetAssetPath(s);
            return string.IsNullOrEmpty(path) ? null : path;
        }

        return null;
    }
}
