using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuntimeDebug
{
    public static class DebugMenu
    {
        private static readonly List<DebugWindow> windows = new();

        public static IReadOnlyList<DebugWindow> Windows => windows;

        public static bool IsVisible { get; private set; }

        public static event Action VisibilityChanged;
        public static event Action Changed;

        public static DebugWindow CreateWindow(string name)
        {
            DebugWindow existing = windows.Find(w => w.Name == name);
            if (existing != null)
            {
                return existing;
            }

            DebugWindow window = new(name);
            windows.Add(window);
            NotifyChanged();
            return window;
        }

        public static void DestroyWindow(string name)
        {
            DebugWindow window = windows.Find(w => w.Name == name);
            if (window != null)
            {
                windows.Remove(window);
                NotifyChanged();
            }
        }

        public static void Toggle()
        {
            SetVisible(!IsVisible);
            Debug.Log($"DebugMenu Toggle : {IsVisible}");
        }

        public static void SetVisible(bool visible)
        {
            if (IsVisible == visible)
            {
                return;
            }

            IsVisible = visible;
            VisibilityChanged?.Invoke();
        }

        public static void DrawContents()
        {
            foreach (DebugWindow window in windows)
            {
                window.Draw();
                GUILayout.Space(8);
            }
        }

        public static void Draw()
        {
            if (!IsVisible)
            {
                return;
            }

            //DrawContents();
        }

        public static void Clear()
        {
            windows.Clear();
            NotifyChanged();
        }

        internal static void NotifyChanged()
        {
            Changed?.Invoke();
        }
    }
}
