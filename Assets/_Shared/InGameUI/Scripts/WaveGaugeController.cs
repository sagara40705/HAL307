using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class WaveGaugeController : MonoBehaviour
{
    private const string DebugWindowName = "Stage";

    [SerializeField] private Material waveMaterial;
    [SerializeField] private string propertyName = "_BaseLine";
    [SerializeField] private float maxValue = 1.0f;
    [SerializeField] private float minValue = 0.0f;
    [SerializeField] private StageEndJudge stageEndJudge;
    [SerializeField] private InGameFlowController flowController;
    [SerializeField] private ScoreManager scoreManager;

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
        CreateDebugMenu();
    }

    private void OnDestroy()
    {
        if (stageEndJudge != null)
        {
            stageEndJudge.OnStageEnd -= OnStageEnd;
        }

        DestroyDebugMenu();
    }

    private void Update()
    {
        if (stageEndJudge == null)
        {
            SetupStageEndJudge();
        }

        if (StageProgressProvider.Instance == null)
        {
            return;
        }

        currentValue =
            Mathf.Lerp(
                minValue,
                maxValue,
                StageProgressProvider.Instance.GaugeRate
            );

        if (waveMaterial != null)
        {
            waveMaterial.SetFloat(
                propertyName,
                currentValue
            );
        }

        if (warningUI != null)
        {
            warningUI.SetActive(
                currentValue <= warningThreshold
            );
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

    public void ForceWaveClear()
    {
        if (!GiveCurrentWaveTargetScore())
        {
            return;
        }

        ForceWaveEnd();
    }

    public void ForceWaveFailed()
    {
        ForceWaveEnd();
    }

    private void ForceWaveEnd()
    {
        SetupStageEndJudge();
        if (stageEndJudge == null)
        {
            Debug.LogWarning("[WaveGaugeController] Force wave end failed. Reason=StageEndJudge is null.");
            return;
        }

        stageEndJudge.ForceStageEndWithoutScore();
    }

    private bool GiveCurrentWaveTargetScore()
    {
        ResolveFlowController();
        WaveData currentWave = flowController != null ? flowController.CurrentWaveData : null;
        if (currentWave == null)
        {
            Debug.LogWarning("[WaveGaugeController] Force wave clear failed. Reason=CurrentWaveData is null.");
            return false;
        }

        ResolveScoreManager();
        if (scoreManager == null)
        {
            Debug.LogWarning("[WaveGaugeController] Force wave clear failed. Reason=ScoreManager is null.");
            return false;
        }

        scoreManager.SetPlayerScoreAtLeast(currentWave.TargetScore);
        Debug.Log($"[WaveGaugeController] Player score forced to target. TargetScore={currentWave.TargetScore}");
        return true;
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

    private void ResolveFlowController()
    {
        if (flowController != null)
        {
            return;
        }

        flowController = FindFirstObjectByType<InGameFlowController>();
    }

    private void ResolveScoreManager()
    {
        if (scoreManager != null)
        {
            return;
        }

        scoreManager = ScoreManager.Instance != null ? ScoreManager.Instance : FindFirstObjectByType<ScoreManager>();
    }

    private void CreateDebugMenu()
    {
        if (!Application.isPlaying) { return; }

        RuntimeDebug.DebugWindow window = RuntimeDebug.DebugMenu.CreateWindow(DebugWindowName);
        window.CreateOrReplaceButton("Force Wave Clear", ForceWaveClear);
        window.CreateOrReplaceButton("Force Wave Failed", ForceWaveFailed);
    }

    private void DestroyDebugMenu()
    {
        if (!Application.isPlaying) { return; }

        RuntimeDebug.DebugMenu.DestroyWindow(DebugWindowName);
    }
}
