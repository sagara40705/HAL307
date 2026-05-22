using System;
using UnityEngine;

namespace RuntimeDebug
{
    public enum DebugItemKind
    {
        Bool,
        Int,
        Float,
        Vector3,
    }


    public abstract class DebugItem
    {
        public string Name { get; }
        public DebugItemKind Kind { get; }

        protected DebugItem(string name, DebugItemKind kind)
        {
            Name = name;
            Kind = kind;
        }

        public abstract void Draw();
    }

    public sealed class DebugBoolItem : DebugItem
    {
        private readonly Func<bool> getter;
        private readonly Action<bool> setter;

        public DebugBoolItem(string name, Func<bool> getter, Action<bool> setter) : base(name, DebugItemKind.Bool)
        {
            this.getter = getter;
            this.setter = setter;
        }

        public override void Draw()
        {
            bool current = getter.Invoke();
            bool next = GUILayout.Toggle(current, Name);

            if (next != current)
            {
                setter.Invoke(next);
            }
        }
    }

    public sealed class DebugIntItem : DebugItem
    {
        private readonly Func<int> getter;
        private readonly Action<int> setter;

        public DebugIntItem(string name, Func<int> getter, Action<int> setter) : base(name, DebugItemKind.Int)
        {
            this.getter = getter;
            this.setter = setter;
        }

        public override void Draw()
        {
            GUILayout.BeginHorizontal();

            GUILayout.Label(Name, GUILayout.Width(140));

            int current = getter.Invoke();
            string text = GUILayout.TextField(current.ToString(), GUILayout.Width(100));

            if (int.TryParse(text, out int next) && next != current)
            {
                setter.Invoke(next);
            }

            GUILayout.EndHorizontal();
        }
    }

    public sealed class DebugFloatItem : DebugItem
    {
        private readonly Func<float> getter;
        private readonly Action<float> setter;

        public DebugFloatItem(string name, Func<float> getter, Action<float> setter) : base(name, DebugItemKind.Float)
        {
            this.getter = getter;
            this.setter = setter;
        }

        public override void Draw()
        {
            GUILayout.BeginHorizontal();

            GUILayout.Label(Name, GUILayout.Width(140));

            float current = getter.Invoke();
            string text = GUILayout.TextField(current.ToString("0.###"), GUILayout.Width(100));

            if (float.TryParse(text, out float next) && !Mathf.Approximately(next, current))
            {
                setter.Invoke(next);
            }

            GUILayout.EndHorizontal();

        }
    }

    public sealed class DebugVector3Item : DebugItem
    {
        private readonly Func<Vector3> getter;
        private readonly Action<Vector3> setter;

        private string xText; private string yText; private string zText;

        public DebugVector3Item(string name, Func<Vector3> getter, Action<Vector3> setter) : base(name, DebugItemKind.Vector3)
        {
            this.getter = getter;
            this.setter = setter;

            Vector3 current = getter.Invoke();

            xText = current.x.ToString("0.###");
            yText = current.y.ToString("0.###");
            zText = current.z.ToString("0.###");
        }

        public override void Draw()
        {
            Vector3 current = getter.Invoke();

            GUILayout.BeginVertical();

            GUILayout.Label(Name);

            GUILayout.BeginHorizontal();

            GUILayout.Label("X", GUILayout.Width(20));
            string nextXText = GUILayout.TextField(xText, GUILayout.Width(70));

            GUILayout.Label("Y", GUILayout.Width(20));
            string nextYText = GUILayout.TextField(yText, GUILayout.Width(70));

            GUILayout.Label("Z", GUILayout.Width(20));
            string nextZText = GUILayout.TextField(zText, GUILayout.Width(70));

            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            xText = nextXText; yText = nextYText; zText = nextZText;

            bool parsedX = float.TryParse(xText, out float x);
            bool parsedY = float.TryParse(yText, out float y);
            bool parsedZ = float.TryParse(zText, out float z);

            if (!parsedX || !parsedY || !parsedZ) { return; }

            Vector3 next = new Vector3(x, y, z);
            if (!Approximately(next, current))
            {
                setter.Invoke(next);
            }
        }

        private static bool Approximately(Vector3 a, Vector3 b)
        {
            return Mathf.Approximately(a.x, b.x)
                && Mathf.Approximately(a.y, b.y)
                && Mathf.Approximately(a.z, b.z);
        }
    }
}
