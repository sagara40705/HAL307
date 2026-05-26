using System.Collections.Generic;
using UnityEngine;

namespace RuntimeDebug
{
    public static class DebugMenu
    {
        private static readonly List<DebugWindow> windows = new();

        public static bool IsVisible { get; private set; }

        public static DebugWindow CreateWindow(string name)
        {
            DebugWindow existing = windows.Find(w => w.Name == name);
            if (existing != null)
            {
                return existing;
            }

            DebugWindow window = new(name);
            windows.Add(window);
            return window;
        }

        public static void DestroyWindow(string name)
        {
            DebugWindow window = windows.Find(w => w.Name == name);
            if (window != null)
            {
                windows.Remove(window);
            }
        }

        public static void Toggle()
        {
            IsVisible = !IsVisible;
            Debug.Log($"DebugMenu Toggle : {IsVisible}");
        }

        public static void Draw()
        {
            if (!IsVisible)
            {
                return;
            }

            GUILayout.BeginArea(new Rect(10, 10, 320, 900));

            foreach (DebugWindow window in windows)
            {
                window.Draw();
                GUILayout.Space(8);
            }

            GUILayout.EndArea();
        }

        public static void Clear()
        {
            windows.Clear();
        }
    }
}
