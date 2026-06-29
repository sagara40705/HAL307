using UnityEngine;

namespace Enemy
{
    [DisallowMultipleComponent]
    public class EnemyParryLightController : MonoBehaviour
    {
        [SerializeField] private Enemy _enemy;
        [SerializeField] private Vector3 _offset = new Vector3(0f, 2f, 0f);
        [SerializeField] private Vector3 _rotationEuler = new Vector3(90f, 0f, 0f);
        [SerializeField] private bool _followEnemy = true;

        [SerializeField, Min(0f)] private float _intensity = 10f;
        [SerializeField, Min(0f)] private float _range = 20f;
        [SerializeField, Range(1f, 179f)] private float _spotAngle = 60f;
        [SerializeField, Range(1f, 179f)] private float _innerSpotAngle = 30f;
        [SerializeField] private LightShadows _shadows = LightShadows.Soft;
        [SerializeField, Range(0f, 1f)] private float _shadowStrength = 1.0f;
        [SerializeField, Range(0f, 2f)] private float _shadowBias = 0.05f;
        [SerializeField, Range(0f, 3f)] private float _shadowNormalBias = 0.4f;

        private GameObject _lightObject;
        private Light _currentSpotLight;

        private void Awake()
        {
            SetupEnemy();
        }

        private void OnDisable()
        {
            DeleteLight();
        }

        private void OnDestroy()
        {
            DeleteLight();
        }

        private void LateUpdate()
        {
            if (_enemy == null)
            {
                SetupEnemy();
            }

            bool shouldLight = _enemy != null && _enemy.PoseState == EnemyPoseState.ParryGrace;
            SetLight(shouldLight);

            if (shouldLight && _followEnemy)
            {
                UpdateLightTransform();
            }
        }

        private void SetupEnemy()
        {
            if (_enemy != null)
            {
                return;
            }

            _enemy = GetComponentInParent<Enemy>();
        }

        private void SetLight(bool enable)
        {
            if (enable)
            {
                CreateSpotLight();
                return;
            }

            DeleteLight();
        }

        private void CreateSpotLight()
        {
            if (_currentSpotLight != null)
            {
                return;
            }

            _lightObject = new GameObject("EnemyParrySpotLight");
            _lightObject.transform.SetParent(_enemy != null ? _enemy.transform : transform);
            UpdateLightTransform();

            _currentSpotLight = _lightObject.AddComponent<Light>();
            _currentSpotLight.type = LightType.Spot;
            _currentSpotLight.intensity = _intensity;
            _currentSpotLight.range = _range;
            _currentSpotLight.shadows = _shadows;
            _currentSpotLight.shadowStrength = _shadowStrength;
            _currentSpotLight.shadowBias = _shadowBias;
            _currentSpotLight.shadowNormalBias = _shadowNormalBias;
            _currentSpotLight.spotAngle = _spotAngle;
            _currentSpotLight.innerSpotAngle = Mathf.Min(_innerSpotAngle, _spotAngle);
        }

        private void UpdateLightTransform()
        {
            if (_lightObject == null)
            {
                return;
            }

            Transform targetTransform = _enemy != null ? _enemy.transform : transform;
            _lightObject.transform.position = targetTransform.position + _offset;
            _lightObject.transform.rotation = Quaternion.Euler(_rotationEuler);
        }

        private void DeleteLight()
        {
            if (_lightObject == null)
            {
                return;
            }

            Destroy(_lightObject);
            _lightObject = null;
            _currentSpotLight = null;
        }
    }
}
