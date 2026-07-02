using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class WaveGaugeController : MonoBehaviour
{
    [SerializeField] private Material waveMaterial;
    [SerializeField] private float decreaseSpeed = 0.1f;
    [SerializeField] private string propertyName = "_BaseLine";
    [SerializeField] private float maxValue = 1.0f;
    [SerializeField] private float minValue = 0.0f;

    [Header("警告UI")]
    [SerializeField] private GameObject warningUI;
    [SerializeField] private float warningThreshold = 0.2f;

    [Header("Wave演出時に非表示にするUI")]
    [SerializeField] private GameObject[] hideOnWaveEffectUis;

    private float currentValue;

    // WaveGaugeが0になった時に何度も演出が出ないようにするフラグ
    private bool isTransitionStarted = false;

    void Start()
    {
        if (waveMaterial == null)
        {
            Image image = GetComponent<Image>();
            if (image != null)
            {
                waveMaterial = image.material;
            }
        }

        if (waveMaterial != null)
        {
            currentValue = maxValue;
            waveMaterial.SetFloat(propertyName, currentValue);
        }
        else
        {
            Debug.LogError("WaveMaterialが見つかりません。");
        }

        if (warningUI != null)
        {
            warningUI.SetActive(false);
        }
    }

    void Update()
    {
        if (waveMaterial != null && currentValue > minValue)
        {
            currentValue -= decreaseSpeed * Time.deltaTime;
            currentValue = Mathf.Max(minValue, currentValue);

            waveMaterial.SetFloat(propertyName, currentValue);

            // ゲージが少なくなったら警告UIを表示
            if (warningUI != null)
            {
                warningUI.SetActive(currentValue <= warningThreshold);
            }

            // WaveGaugeが0になったらシーン遷移演出を開始
            if (!isTransitionStarted && currentValue <= minValue)
            {
                isTransitionStarted = true;

                // ここで他のUIを非表示
                HideOtherUis();

                PlayGaugeEmptyTransition().Forget();
            }
        }
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
    }
}
