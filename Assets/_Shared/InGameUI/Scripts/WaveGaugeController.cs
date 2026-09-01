using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using static Sound.Manager.SoundManager;

public class WaveGaugeController : MonoBehaviour
{
    // 
    [SerializeField] private Material waveMaterial;
    [SerializeField] private string propertyName = "_BaseLine";
    [SerializeField] private float maxValue = 1.0f;
    [SerializeField] private float minValue = 0.0f;
    [SerializeField] private StageEndJudge stageEndJudge;

    [Header("警告UI")]
    [SerializeField] private GameObject warningUI;
    [SerializeField] private float warningThreshold = 0.2f;
    [SerializeField] private Animator warningAnimator;
    private bool isWarningVisible;
    private bool isWaveFinalSuppressed;

    [Header("Wave演出時に非表示にするUI")]
    [SerializeField] private GameObject[] hideOnWaveEffectUis;

    [SerializeField] private AdjustableUiPresenter warningPresenter;

    private float currentValue;
    private bool isTransitionStarted;

    private void Start()
    {
        // 初期化処理
        if (waveMaterial == null)
        {
            // Imageコンポーネントからマテリアルを取得
            Image image = GetComponent<Image>();
            if (image != null)
            {
                // マテリアルを取得
                waveMaterial = image.material;
            }
        }

        // 初期値を設定
        if (waveMaterial != null)
        {
            // マテリアルのプロパティに初期値を設定
            currentValue = maxValue;
            waveMaterial.SetFloat(propertyName, currentValue);
        }
        else
        {
            Debug.LogError("WaveMaterial was not found.");
        }

        // 警告UIを非表示にする
        if (warningAnimator == null && warningUI != null)
        {
            warningAnimator = warningUI.GetComponent<Animator>();
        }

        if (warningPresenter == null && warningUI != null)
        {
            warningPresenter = warningUI.GetComponent<AdjustableUiPresenter>();
        }

        isWarningVisible = false;
        HideWarningUi(true);

        // StageEndJudgeの設定
        SetupStageEndJudge();
    }

    private void OnDestroy()
    {
        // StageEndJudgeのイベント購読を解除
        if (stageEndJudge != null)
        {
            // イベントの購読を解除
            stageEndJudge.OnStageEnd -= OnStageEnd;
        }
    }

    private void Update()
    {
        // StageEndJudgeが設定されていない場合は設定する
        if (stageEndJudge == null)
        {
            SetupStageEndJudge();
        }

        // StageProgressProviderが存在しない場合は処理を中断
        if (StageProgressProvider.Instance == null)
        {
            return;
        }

        // StageProgressProviderのGaugeRateに基づいてcurrentValueを更新
        currentValue =
            Mathf.Lerp(
                minValue,
                maxValue,
                StageProgressProvider.Instance.GaugeRate
            );

        // マテリアルのプロパティにcurrentValueを設定
        if (waveMaterial != null)
        {
            waveMaterial.SetFloat(
                propertyName,
                currentValue
            );
        }

        // 警告UIの表示状態を更新
        bool shouldShowWarning = !isWaveFinalSuppressed && currentValue <= warningThreshold;

        // 状態が変わった時だけ処理する
        if (isWarningVisible != shouldShowWarning)
        {
            SetWaveFinalVisible(shouldShowWarning);

            if (shouldShowWarning)
            {
                HideOtherUis();
            }
            else
            {
                ShowOtherUis();
            }
        }
    }

    public void ResetWaveGauge()
    {
        // TransitionManagerの状態をリセット
        TransitionManager.ResetImmediate();

        // WaveGaugeControllerの状態をリセット
        isTransitionStarted = false;
        isWaveFinalSuppressed = false;
        currentValue = maxValue;

        // マテリアルが設定されていない場合はImageコンポーネントから取得
        if (waveMaterial == null)
        {
            Image image = GetComponent<Image>();
            if (image != null)
            {
                waveMaterial = image.material;
            }
        }

        // マテリアルのプロパティにcurrentValueを設定
        if (waveMaterial != null)
        {
            waveMaterial.SetFloat(propertyName, currentValue);
        }

        // 警告UIを非表示にする
        SetWaveFinalVisible(false);

        ShowOtherUis();
    }

    public void HideWaveFinalForGameEnd()
    {
        isWaveFinalSuppressed = true;
        SetWaveFinalVisible(false);
        ShowOtherUis();
    }

    private void SetupStageEndJudge()
    {
        // StageEndJudgeが既に設定されている場合は処理を中断
        if (stageEndJudge != null)
        {
            return;
        }

        // StageModelContextを取得し、StageEndJudgeを設定
        StageModelContext stageModelContext = FindFirstObjectByType<StageModelContext>();
        stageEndJudge = stageModelContext != null ? stageModelContext.StageEndJudge : FindFirstObjectByType<StageEndJudge>();
        if (stageEndJudge == null)
        {
            return;
        }

        // StageEndJudgeのOnStageEndイベントにOnStageEndメソッドを登録
        stageEndJudge.OnStageEnd -= OnStageEnd;
        stageEndJudge.OnStageEnd += OnStageEnd;
    }

    private void OnStageEnd()
    {
        // StageEndJudgeのイベントが発火した際の処理
        if (isTransitionStarted)
        {
            return;
        }

        // TransitionManagerの状態をリセット
        isTransitionStarted = true;
        // WaveGaugeControllerの状態をリセット
        currentValue = minValue;

        // マテリアルが設定されていない場合はImageコンポーネントから取得
        if (waveMaterial != null)
        {
            waveMaterial.SetFloat(propertyName, currentValue);
        }

        // 警告UIの表示状態を更新
        SetWaveFinalVisible(true);

        HideOtherUis();

        PlayGaugeEmptyTransition().Forget();
    }

    private void HideOtherUis()
    {
        // Wave演出時に非表示にするUIを非表示設定にする
        foreach (GameObject ui in hideOnWaveEffectUis)
        {
            if (ui != null)
            {
                ui.SetActive(false);
            }
        }
    }

    // Wave演出終了後に非表示にしたUIを再表示する
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

    private void SetWaveFinalVisible(bool isVisible, bool immediateHide = false)
    {
        isWarningVisible = isVisible;

        if (warningAnimator != null)
        {
            warningAnimator.SetBool("IsWarning", isVisible);
        }

        if (isVisible)
        {
            ShowWarningUi();
        }
        else
        {
            HideWarningUi(immediateHide);
        }
    }

    // Wave演出のトランジションを再生する非同期メソッド
    private async UniTaskVoid PlayGaugeEmptyTransition()
    {
        await TransitionManager.PlayDumbbellFadeOut();
    }

    private void ShowWarningUi()
    {
        if (warningPresenter != null)
        {
            warningPresenter.CaptureCurrentAsBase();
            warningPresenter.Show();
            return;
        }

        if (warningUI != null)
        {
            warningUI.SetActive(true);
        }
    }

    private void HideWarningUi(bool immediate = false)
    {
        if (warningPresenter != null)
        {
            if (immediate)
            {
                warningPresenter.HideImmediate();
            }
            else
            {
                warningPresenter.Hide();
            }

            return;
        }

        if (warningUI != null)
        {
            warningUI.SetActive(false);
        }
    }
}
