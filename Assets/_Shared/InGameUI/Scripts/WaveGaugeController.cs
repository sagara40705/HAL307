using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class WaveGaugeController : MonoBehaviour
{
    [SerializeField] private Material waveMaterial;
    [SerializeField] private string propertyName = "_BaseLine";
    [SerializeField] private float maxValue = 1.0f;
    [SerializeField] private float minValue = 0.0f;
    [SerializeField] private StageEndJudge stageEndJudge;

    [Header("警告UI")]
    [SerializeField] private GameObject warningUI;
    [SerializeField] private float warningThreshold = 0.2f;

    [Header("Wave演出時に非表示にするUI")]
    [SerializeField] private GameObject[] hideOnWaveEffectUis;

    private float currentValue;
    private bool isTransitionStarted;

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

        if (waveMaterial != null)
        {
            currentValue = maxValue;
            waveMaterial.SetFloat(propertyName, currentValue);
        }
        else
        {
            Debug.LogError("WaveMaterial was not found.");
        }

        if (warningUI != null)
        {
            warningUI.SetActive(false);
        }

        SetupStageEndJudge();
    }

    private void OnDestroy()
    {
        if (stageEndJudge == null)
        {
            return;
        }

        stageEndJudge.OnStageEnd -= OnStageEnd;
    }

    private void Update()
    {
        if (stageEndJudge == null)
        {
            SetupStageEndJudge();
        }
    }

    public void ResetWaveGauge()
    {
        TransitionManager.ResetImmediate();

        isTransitionStarted = false;
        currentValue = maxValue;

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
            waveMaterial.SetFloat(propertyName, currentValue);
        }

        if (warningUI != null)
        {
            warningUI.SetActive(false);
        }

        ShowOtherUis();
    }

    private void SetupStageEndJudge()
    {
        if (stageEndJudge != null)
        {
            return;
        }

        StageModelContext stageModelContext = FindFirstObjectByType<StageModelContext>();
        stageEndJudge = stageModelContext != null ? stageModelContext.StageEndJudge : FindFirstObjectByType<StageEndJudge>();
        if (stageEndJudge == null)
        {
            return;
        }

        stageEndJudge.OnStageEnd -= OnStageEnd;
        stageEndJudge.OnStageEnd += OnStageEnd;
    }

    private void OnStageEnd()
    {
        if (isTransitionStarted)
        {
            return;
        }

        isTransitionStarted = true;
        currentValue = minValue;

        if (waveMaterial != null)
        {
            waveMaterial.SetFloat(propertyName, currentValue);
        }

        if (warningUI != null)
        {
            warningUI.SetActive(currentValue <= warningThreshold);
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

    private void ShowOtherUis()
    {
        foreach (GameObject ui in hideOnWaveEffectUis)
        {
            if (ui != null)
            {
                ui.SetActive(true);
            }
        }
    }

    private async UniTaskVoid PlayGaugeEmptyTransition()
    {
        await TransitionManager.PlayDumbbellFadeOut();
    }
}
