using UnityEngine;
using UnityEngine.InputSystem;

namespace RuntimeDebug
{
    public sealed class DebugMenuRunner : MonoBehaviour
    {
        private void Update()
        {
            if (DebugShortcuts.Combo(Key.D, Key.M))
            {
                DebugMenu.Toggle();
            }
        }

        private void OnGUI()
        {
            //float scale = 1.5f;
            //Matrix4x4 oldMatrix = GUI.matrix;

            //GUIUtility.ScaleAroundPivot(new Vector2(scale, scale), Vector2.zero);

            DebugMenu.Draw();

            //GUI.matrix = oldMatrix;
        }
    }
}
