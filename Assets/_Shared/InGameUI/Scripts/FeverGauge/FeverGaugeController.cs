using System;
using System.Collections;
using UnityEngine;
using static Sound.Manager.SoundManager;

public class FeverGaugeController : MonoBehaviour
{
    private const string DebugWindowName = "Fever Gauge";
    private const float DefaultFeverDuration = 10f;
    private const float DefaultCutInDisplayTime = 3f;
    private const bool DefaultResetFillOnFeverEnd = true;

    [SerializeField] private Material _flameMaterial;
    [SerializeField] private string _fillAmountProperty = "_FillAmount";
    [SerializeField] private float DefaultFillIncrease = 0.05f;
    [SerializeField] private FeverSettings _settings;

    [Header("フィーバー演出")]
    [SerializeField] private GameObject _fireAnimObject;
    [SerializeField] private GameObject _cutInObject;

    [Header("フィーバーUI出方調整")]
    [SerializeField] private AdjustableUiPresenter _firePresenter;
    [SerializeField] private AdjustableUiPresenter _cutInPresenter;
    [SerializeField] private AdjustableUiPresenter _uiPanelPresenter;

    //feverの枠
    [SerializeField] private GameObject uiPanel;

    private float _feverTimer;
    private bool _isFever;
    private Coroutine _cutInCoroutine;

    public bool IsFever => _isFever;
    public float FeverDuration => _settings != null
        ? _settings.FeverDuration
        : DefaultFeverDuration;
    public float FeverRemainingTime => _feverTimer;
    public float CurrentFill => GetFill();

    public event Action OnFeverStarted;
    public event Action OnFeverEnded;

    private void Awake()
    {
        if (_settings == null)
        {
            Debug.LogWarning("[FeverGaugeController] FeverSettings is not assigned. Default fever values are used.", this);
        }

        Debug.Log($"[FeverGaugeController] Awake前 CutIn activeSelf = {_cutInObject?.activeSelf}", this);

        HideFeverObjects();

        Debug.Log($"[FeverGaugeController] Awake後 CutIn activeSelf = {_cutInObject?.activeSelf}", this);
    }

    private void Start()
    {
        CachePresenters();

        ResetFill();
        CreateDebugMenu();

        Debug.Log($"[FeverGaugeController] Start後 CutIn activeSelf = {_cutInObject?.activeSelf}", this);

        if (!_isFever)
        {
            HideFeverObjects();
            Debug.Log($"[FeverGaugeController] Startで再非表示 CutIn activeSelf = {_cutInObject?.activeSelf}", this);
        }

    }

    private void HideFeverObjects()
    {
        HideUi(_fireAnimObject, _firePresenter, true);
        HideUi(_cutInObject, _cutInPresenter, true);
        HideUi(uiPanel, _uiPanelPresenter, true);
    }

    private void ShowUi(GameObject obj, AdjustableUiPresenter presenter)
    {
        if (presenter != null)
        {
            presenter.CaptureCurrentAsBase();
            presenter.Show();
            return;
        }

        if (obj != null)
        {
            obj.SetActive(true);
        }
    }

    private void HideUi(GameObject obj, AdjustableUiPresenter presenter, bool immediate = false)
    {
        if (presenter != null)
        {
            if (immediate)
            {
                presenter.HideImmediate();
            }
            else
            {
                presenter.Hide();
            }

            return;
        }

        if (obj != null)
        {
            obj.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        RuntimeDebug.DebugMenu.DestroyWindow(DebugWindowName);
    }

    private void Update()
    {
        if (!_isFever)
        {
            return;
        }

        _feverTimer -= Time.deltaTime;

        if (_feverTimer <= 0f)
        {
            EndFever();
        }
    }

    public void AddFill()
    {
        if (_isFever)
        {
            return;
        }

        float fill = Mathf.Clamp01(GetFill() + FillIncrease);
        SetFill(fill);

        if (fill >= 1f)
        {
            StartFever();
        }
    }

    public void ResetFill()
    {
        SetFill(0f);
    }

    public void DebugIncreaseFill()
    {
        AddFill();
    }

    public void DebugDecreaseFill()
    {
        if (_isFever)
        {
            return;
        }

        SetFill(GetFill() - FillIncrease);
    }

    public void DebugResetFill()
    {
        if (_isFever)
        {
            EndFever();
            return;
        }

        ResetFill();
    }

    private void StartFever()
    {
        if (_isFever)
        {
            return;
        }

        _isFever = true;
        _feverTimer = FeverDuration;

        SetFill(1f);

        ShowUi(_fireAnimObject, _firePresenter);
        ShowUi(uiPanel, _uiPanelPresenter);


        Sound.Manager.SoundManager.Stop((int)SceneSoundTag.InGameSound, (int)InGameSoundTag.InGameBGM);
        Sound.Manager.SoundManager.Play((int)SceneSoundTag.InGameSound, (int)InGameSoundTag.FeverBGM);

        PlayCutIn();


        OnFeverStarted?.Invoke();

        Debug.Log("[FeverGaugeController] Fever started.");
    }

    private void EndFever()
    {
        if (!_isFever)
        {
            return;
        }

        _isFever = false;
        _feverTimer = 0f;

        HideUi(_fireAnimObject, _firePresenter);

        if (_cutInCoroutine != null)
        {
            StopCoroutine(_cutInCoroutine);
            _cutInCoroutine = null;
        }

        HideUi(_cutInObject, _cutInPresenter);


        if (ResetFillOnFeverEnd)
        {
            ResetFill();
        }

        HideUi(uiPanel, _uiPanelPresenter);

        Sound.Manager.SoundManager.Stop((int)SceneSoundTag.InGameSound, (int)InGameSoundTag.FeverBGM);
        Sound.Manager.SoundManager.Play((int)SceneSoundTag.InGameSound, (int)InGameSoundTag.InGameBGM);
        OnFeverEnded?.Invoke();

        Debug.Log("[FeverGaugeController] Fever ended.");
    }

    private void PlayCutIn()
    {
        if (_cutInObject == null)
        {
            return;
        }

        if (_cutInCoroutine != null)
        {
            StopCoroutine(_cutInCoroutine);
        }

        _cutInCoroutine = StartCoroutine(PlayCutInCoroutine());
    }

    private IEnumerator PlayCutInCoroutine()
    {
        ShowUi(_cutInObject, _cutInPresenter);

        yield return new WaitForSecondsRealtime(CutInDisplayTime);

        HideUi(_cutInObject, _cutInPresenter);

        _cutInCoroutine = null;
    }

    private float GetFill()
    {
        if (_flameMaterial == null)
        {
            return 0f;
        }

        return Mathf.Clamp01(_flameMaterial.GetFloat(_fillAmountProperty));
    }

    private void SetFill(float fill)
    {
        if (_flameMaterial == null)
        {
            Debug.LogWarning("[FeverGaugeController] Flame material is not assigned.", this);
            return;
        }

        _flameMaterial.SetFloat(_fillAmountProperty, Mathf.Clamp01(fill));
    }

    private void CreateDebugMenu()
    {
        RuntimeDebug.DebugWindow window = RuntimeDebug.DebugMenu.CreateWindow(DebugWindowName);
        window.CreateButton("Fill +", DebugIncreaseFill);
        window.CreateButton("Fill -", DebugDecreaseFill);
        window.CreateButton("Reset Fill", DebugResetFill);
        window.CreateMessage("Current Fill", () => CurrentFill.ToString("0.00"));
        window.CreateMessage("Is Fever", () => IsFever.ToString());
        window.CreateMessage("Fever Time", () => FeverRemainingTime.ToString("0.00"));
    }

    public void SetFillForTutorial(float fill)
    {
        if (_isFever)
        {
            return;
        }

        SetFill(fill);
    }

    public void ForceFeverForTutorial()
    {
        if (_isFever)
        {
            return;
        }

        StartFever();
    }

    public void ResetForTutorial()
    {
        if (_isFever)
        {
            EndFever();
            return;
        }

        ResetFill();
    }

    private void CachePresenters()
    {
        if (_firePresenter == null && _fireAnimObject != null)
        {
            _firePresenter = _fireAnimObject.GetComponent<AdjustableUiPresenter>();
        }

        if (_cutInPresenter == null && _cutInObject != null)
        {
            _cutInPresenter = _cutInObject.GetComponent<AdjustableUiPresenter>();
        }

        if (_uiPanelPresenter == null && uiPanel != null)
        {
            _uiPanelPresenter = uiPanel.GetComponent<AdjustableUiPresenter>();
        }
    }

    private float FillIncrease => _settings != null
        ? _settings.FillIncrease
        : DefaultFillIncrease;

    private bool ResetFillOnFeverEnd => _settings != null
        ? _settings.ResetFillOnFeverEnd
        : DefaultResetFillOnFeverEnd;

    private float CutInDisplayTime => _settings != null
        ? _settings.CutInDisplayTime
        : DefaultCutInDisplayTime;


}

