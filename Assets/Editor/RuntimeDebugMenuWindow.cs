using UnityEditor;
using UnityEngine;

using RuntimeDebugMenu = RuntimeDebug.DebugMenu;

namespace RuntimeDebugEditor
{
    [InitializeOnLoad]
    internal static class RuntimeDebugMenuWindowBridge
    {
        static RuntimeDebugMenuWindowBridge()
        {
            RuntimeDebugMenu.VisibilityChanged += OnVisibilityChanged;
            RuntimeDebugMenu.Changed += RuntimeDebugMenuWindow.RepaintOpenWindows;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnVisibilityChanged()
        {
            if (RuntimeDebugMenu.IsVisible)
            {
                RuntimeDebugMenuWindow.OpenWindow();
                return;
            }

            RuntimeDebugMenuWindow.CloseOpenWindows();
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode)
            {
                RuntimeDebugMenu.Clear();
            }
        }
    }

    public sealed class RuntimeDebugMenuWindow : EditorWindow
    {
        private const string MenuPath = "Tools/Runtime Debug";
        private const int Priority = 140;

        private Vector2 _scroll;

        [MenuItem(MenuPath + "/Open", false, Priority)]
        public static void OpenFromMenu()
        {
            RuntimeDebugMenu.SetVisible(true);
            OpenWindow().Focus();
        }

        [MenuItem(MenuPath + "/Close", false, Priority + 1)]
        public static void CloseFromMenu()
        {
            RuntimeDebugMenu.SetVisible(false);
        }

        internal static RuntimeDebugMenuWindow OpenWindow()
        {
            RuntimeDebugMenuWindow window = GetWindow<RuntimeDebugMenuWindow>("Runtime Debug");
            window.minSize = new Vector2(360f, 240f);
            window.Show();
            return window;
        }

        internal static void CloseOpenWindows()
        {
            foreach (RuntimeDebugMenuWindow window in Resources.FindObjectsOfTypeAll<RuntimeDebugMenuWindow>())
            {
                window.Close();
            }
        }

        internal static void RepaintOpenWindows()
        {
            foreach (RuntimeDebugMenuWindow window in Resources.FindObjectsOfTypeAll<RuntimeDebugMenuWindow>())
            {
                window.Repaint();
            }
        }

        private void OnEnable()
        {
            minSize = new Vector2(360f, 240f);
            EditorApplication.update += RepaintDuringPlayMode;

            if (!RuntimeDebugMenu.IsVisible)
            {
                RuntimeDebugMenu.SetVisible(true);
            }
        }

        private void OnDisable()
        {
            EditorApplication.update -= RepaintDuringPlayMode;
        }

        private void OnDestroy()
        {
            if (RuntimeDebugMenu.IsVisible)
            {
                RuntimeDebugMenu.SetVisible(false);
            }
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (!EditorApplication.isPlaying)
            {
                EditorGUILayout.HelpBox("Runtime debug items are registered while Play Mode is running.", MessageType.Info);
            }

            if (RuntimeDebugMenu.Windows.Count == 0)
            {
                EditorGUILayout.HelpBox("No runtime debug windows are registered.", MessageType.None);
                return;
            }

            using (EditorGUILayout.ScrollViewScope scrollView = new EditorGUILayout.ScrollViewScope(_scroll))
            {
                _scroll = scrollView.scrollPosition;
                RuntimeDebugMenu.DrawContents();
            }
        }

        private static void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                string playModeText = EditorApplication.isPlaying ? "Play Mode" : "Edit Mode";
                GUILayout.Label(playModeText, EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();

                using (new EditorGUI.DisabledScope(RuntimeDebugMenu.Windows.Count == 0))
                {
                    if (GUILayout.Button("Clear", EditorStyles.toolbarButton, GUILayout.Width(52f)))
                    {
                        RuntimeDebugMenu.Clear();
                    }
                }
            }
        }

        private void RepaintDuringPlayMode()
        {
            if (EditorApplication.isPlaying)
            {
                Repaint();
            }
        }
    }
}
