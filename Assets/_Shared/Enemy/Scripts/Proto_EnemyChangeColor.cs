using UnityEngine;

namespace Enemy
{
    public class Proto_EnemyChangeColor : MonoBehaviour
    {
        private Renderer targetRenderer;
        [SerializeField] private Color defaultColor = Color.white;

        private MaterialPropertyBlock propertyBlock;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

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

            //Debug.Log("色を変更しました: " + color);
        }

        public void ResetColor()
        {
            SetColor(defaultColor);
        }
    }
}
