using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class WaveGaugeController : MonoBehaviour
{
    [Header("Waveゲージ")]
    [SerializeField] private Material waveMaterial;
    [SerializeField] private string propertyName = "_BaseLine";
    [SerializeField] private float maxValue = 1.0f;
    [SerializeField] private float minValue = 0.0f;

    [Header("距離で減少させるやつ")]
    [SerializeField] private Transform distanceFromObject;
    [SerializeField] private Transform distanceToObject;
    [SerializeField] private float arriveDistance = 0.2f;

    [Header("警告UI")]
    [SerializeField] private GameObject warningUI;
    [SerializeField] private float warningThreshold = 0.2f;

    [Header("Fade演出時に非表示にするUI")]
    [SerializeField] private GameObject[] hideOnWaveEffectUis;

    private float currentValue;
    private float startDistance;
    private bool isTransitionStarted = false;

    private void Start()
    {
        if (waveMaterial == null)
        {
            Image image = GetComponent<Image>();

            if (image != null)
            {
                waveMaterial = image.material;
            }
        }

        if (waveMaterial == null)
        {
            Debug.LogError("WaveMaterialが見つかりません。");
            return;
        }

        if (distanceFromObject == null || distanceToObject == null)
        {
            Debug.LogError("距離を測るオブジェクトが設定されていません。", this);
            return;
        }

        startDistance = Vector3.Distance(
         distanceFromObject.position,
        distanceToObject.position
        );

        if (startDistance <= arriveDistance)
        {
            Debug.LogWarning(
                "[WaveGaugeController] 開始時点でDistanceFromObjectとDistanceToObjectが近すぎます。距離設定を確認してください。",
                this
            );

            currentValue = maxValue;
            waveMaterial.SetFloat(propertyName, currentValue);

            enabled = false;

            return;
        }
        currentValue = maxValue;
        waveMaterial.SetFloat(propertyName, currentValue);

        if (warningUI != null)
        {
            warningUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (waveMaterial == null)
        {
            return;
        }

        if (distanceFromObject == null || distanceToObject == null)
        {
            return;
        }

        if (isTransitionStarted)
        {
            return;
        }

        float currentDistance = Vector3.Distance(
            distanceFromObject.position,
            distanceToObject.position
        );

        if (currentDistance <= arriveDistance)
        {
            SetGaugeValue(minValue);
            StartGaugeEmptyTransition();
            return;
        }

        float distanceRate = Mathf.InverseLerp(
            arriveDistance,
            startDistance,
            currentDistance
        );

        currentValue = Mathf.Lerp(
            minValue,
            maxValue,
            distanceRate
        );

        currentValue = Mathf.Clamp(
            currentValue,
            minValue,
            maxValue
        );

        SetGaugeValue(currentValue);

        if (warningUI != null)
        {
            warningUI.SetActive(currentValue <= warningThreshold);
        }
    }

    private void SetGaugeValue(float value)
    {
        currentValue = Mathf.Clamp(value, minValue, maxValue);
        waveMaterial.SetFloat(propertyName, currentValue);
    }

    private void StartGaugeEmptyTransition()
    {
        if (isTransitionStarted)
        {
            return;
        }

        isTransitionStarted = true;

        if (warningUI != null)
        {
            warningUI.SetActive(true);
        }

        HideOtherUis();
        PlayGaugeEmptyTransition().Forget();
    }

    private void HideOtherUis()
    {
        foreach (GameObject ui in hideOnWaveEffectUis)
        {
            if (ui != null)
            {
                ui.SetActive(false);
            }
        }
    }

    private async UniTaskVoid PlayGaugeEmptyTransition()
    {
        await TransitionManager.PlayDumbbellFadeOut();
        await UniTask.Delay(2000);

        await TransitionManager.FadeIn();
    }
}
