using UnityEngine;

namespace Player
{
    public class Proto_PlayerColorChanger : MonoBehaviour
    {
        private Renderer targetRenderer;
        [SerializeField] private Color defaultColor = Color.white;

        private void Awake()
        {
            if (targetRenderer == null)
            {
                targetRenderer = GetComponentInChildren<Renderer>();
            }

            SetColor(defaultColor);
        }

        public void SetColor(Color color)
        {
            if (targetRenderer == null)
            {
                Debug.LogWarning("Rendererが見つかりません。色の変更ができません。");
                return;
            }

            var material = targetRenderer.material;

            material.color = color;

            Debug.Log("色を変更しました: " + color);
        }

        public void ResetColor()
        {
            SetColor(defaultColor);
        }
    }
}
