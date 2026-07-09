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

    [Header("警告UI")]
    [SerializeField] private GameObject warningUI;
    [SerializeField] private float warningThreshold = 0.05f;

    [Header("Fade演出時に非表示にするUI")]
    [SerializeField] private GameObject[] hideOnWaveEffectUis;

    [Header("フェード設定")]
    [SerializeField] private float fadeInDelay = 2f;
    [SerializeField] private float fadeThreshold = 0.035f;

    private float currentValue;
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
            Debug.LogError("[WaveGaugeController] WaveMaterial が見つかりません。", this);
            enabled = false;
            return;
        }

        currentValue = maxValue;
        SetGaugeValue(currentValue);

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

        if (isTransitionStarted)
        {
            return;
        }

        StageProgressProvider provider = StageProgressProvider.Instance;

        if (provider == null)
        {
            return;
        }

        float gaugeRate = Mathf.Clamp01(provider.GaugeRate);

        currentValue = Mathf.Lerp(
            minValue,
            maxValue,
            gaugeRate
        );

        currentValue = Mathf.Clamp(
            currentValue,
            minValue,
            maxValue
        );

        SetGaugeValue(currentValue);

        Debug.Log($"[WaveGaugeController] gaugeRate={gaugeRate}, currentValue={currentValue}, IsArrived={provider.IsArrived}",
            this);

        // 警告UIは GaugeRate 基準で判定する
        if (warningUI != null)
        {
            warningUI.SetActive(currentValue <= warningThreshold);
        }

        if (provider.IsArrived || currentValue <= fadeThreshold)
        {
            SetGaugeValue(minValue);
            StartGaugeEmptyTransition();
        }
    }

    private void SetGaugeValue(float value)
    {
        currentValue = Mathf.Clamp(
            value,
            minValue,
            maxValue
        );

        waveMaterial.SetFloat(
            propertyName,
            currentValue
        );
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

        await UniTask.Delay(
            (int)(fadeInDelay * 1000)
        );

        await TransitionManager.FadeIn();
    }
}
