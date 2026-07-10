using System;
using System.Collections.Generic;
using UnityEngine;

namespace RuntimeDebug
{
    public sealed class DebugWindow
    {
        public string Name { get; }

        private bool isExpanded;
        private readonly List<DebugItem> items = new();

        public DebugWindow(string name)
        {
            Name = name;
        }

        public void CreateBind(string name, Func<bool> getter, Action<bool> setter)
        {
            items.Add(new DebugBoolItem(name, getter, setter));
            DebugMenu.NotifyChanged();
        }

        public void CreateBind(string name, Func<int> getter, Action<int> setter)
        {
            items.Add(new DebugIntItem(name, getter, setter));
            DebugMenu.NotifyChanged();
        }

        public void CreateBind(string name, Func<float> getter, Action<float> setter)
        {
            items.Add(new DebugFloatItem(name, getter, setter));
            DebugMenu.NotifyChanged();
        }

        public void CreateBind(string name, Func<Vector3> getter, Action<Vector3> setter)
        {
            items.Add(new DebugVector3Item(name, getter, setter));
            DebugMenu.NotifyChanged();
        }

        public void CreateButton(string name, Action onClick)
        {
            items.Add(new DebugButtonItem(name, onClick));
            DebugMenu.NotifyChanged();
        }

        public void CreateMessage(string name, Func<string> getter)
        {
            items.Add(new DebugMessageItem(name, getter));
            DebugMenu.NotifyChanged();
        }

        public void Draw()
        {
            if (GUILayout.Button(Name))
            {
                isExpanded = !isExpanded;
            }

            if (!isExpanded)
            {
                return;
            }

            GUILayout.BeginVertical(GUI.skin.window);

            foreach (DebugItem item in items)
            {
                item.Draw();
            }

            GUILayout.EndVertical();
        }
    }
}
