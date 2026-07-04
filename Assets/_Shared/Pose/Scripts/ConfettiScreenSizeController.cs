using UnityEngine;

namespace Pose
{
    [DisallowMultipleComponent]
    public sealed class ConfettiScreenSizeController : MonoBehaviour
    {
        [SerializeField] private Camera targetCamera;
        [SerializeField, Min(0.01f)] private float referenceParticleWorldSize = 1f;
        [SerializeField, Range(0.01f, 0.5f)] private float targetViewportHeight = 0.12f;
        [SerializeField] private bool updateEveryFrame = true;

        private Vector3 _baseScale;

        private void Awake()
        {
            _baseScale = transform.localScale;
            ApplyScreenSize();
        }

        private void LateUpdate()
        {
            if (updateEveryFrame)
            {
                ApplyScreenSize();
            }
        }

        public void ApplyScreenSize()
        {
            Camera camera = ResolveCamera();
            if (camera == null)
            {
                return;
            }

            float viewHeight = GetViewHeightAtEffect(camera);
            if (viewHeight <= 0f)
            {
                return;
            }

            float targetWorldSize = viewHeight * targetViewportHeight;
            float scale = targetWorldSize / referenceParticleWorldSize;
            transform.localScale = _baseScale * scale;
        }

        private Camera ResolveCamera()
        {
            if (targetCamera != null && targetCamera.isActiveAndEnabled)
            {
                return targetCamera;
            }

            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            Camera bestCamera = null;
            foreach (Camera camera in cameras)
            {
                if (camera == null || !camera.isActiveAndEnabled)
                {
                    continue;
                }

                if (!IsVisibleFrom(camera))
                {
                    continue;
                }

                if (bestCamera == null || camera.depth > bestCamera.depth)
                {
                    bestCamera = camera;
                }
            }

            if (bestCamera != null)
            {
                return bestCamera;
            }

            Camera mainCamera = Camera.main;
            if (mainCamera != null && mainCamera.isActiveAndEnabled)
            {
                return mainCamera;
            }

            return null;
        }

        private bool IsVisibleFrom(Camera camera)
        {
            Vector3 viewportPosition = camera.WorldToViewportPoint(transform.position);
            return viewportPosition.z > camera.nearClipPlane
                && viewportPosition.x >= 0f
                && viewportPosition.x <= 1f
                && viewportPosition.y >= 0f
                && viewportPosition.y <= 1f;
        }

        private float GetViewHeightAtEffect(Camera camera)
        {
            if (camera.orthographic)
            {
                return camera.orthographicSize * 2f;
            }

            Vector3 toEffect = transform.position - camera.transform.position;
            float distance = Vector3.Dot(toEffect, camera.transform.forward);
            if (distance <= camera.nearClipPlane)
            {
                distance = Vector3.Distance(transform.position, camera.transform.position);
            }

            return 2f * distance * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        }
    }
}
