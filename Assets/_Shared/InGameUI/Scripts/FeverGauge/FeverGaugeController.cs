using System;
using System.Collections;
using UnityEngine;

public class FeverGaugeController : MonoBehaviour
{
    private const string DebugWindowName = "Fever Gauge";

    [SerializeField] private Material _flameMaterial;
    [SerializeField] private string _fillAmountProperty = "_FillAmount";
    [SerializeField] private float _fillIncrease = 0.05f;
    [SerializeField] private float _feverDuration = 10f;
    [SerializeField] private bool _resetFillOnFeverEnd = true;

    [Header("フィーバー演出")]
    [SerializeField] private GameObject _fireAnimObject;
    [SerializeField] private GameObject _cutInObject;
    [SerializeField] private float _cutInDisplayTime = 3f;

    //feverの枠
    [SerializeField] private GameObject uiPanel;

    private float _feverTimer;
    private bool _isFever;
    private Coroutine _cutInCoroutine;

    public bool IsFever => _isFever;
    public float FeverDuration => _feverDuration;
    public float FeverRemainingTime => _feverTimer;
    public float CurrentFill => GetFill();

    public event Action OnFeverStarted;
    public event Action OnFeverEnded;

    private void Start()
    {
        ResetFill();

        if (_fireAnimObject != null)
        {
            _fireAnimObject.SetActive(false);
        }

        if (_cutInObject != null)
        {
            _cutInObject.SetActive(false);
        }

        if (uiPanel != null)
        {
            uiPanel.SetActive(false);
        }

        CreateDebugMenu();
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

        float fill = Mathf.Clamp01(GetFill() + _fillIncrease);
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

        SetFill(GetFill() - _fillIncrease);
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
        _feverTimer = Mathf.Max(0f, _feverDuration);

        SetFill(1f);

        if (_fireAnimObject != null)
        {
            _fireAnimObject.SetActive(true);
        }

        if (uiPanel != null)
        {
            uiPanel.SetActive(true);
        }

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

        if (_fireAnimObject != null)
        {
            _fireAnimObject.SetActive(false);
        }

        if (_cutInCoroutine != null)
        {
            StopCoroutine(_cutInCoroutine);
            _cutInCoroutine = null;
        }

        if (_cutInObject != null)
        {
            _cutInObject.SetActive(false);
        }

        if (_resetFillOnFeverEnd)
        {
            ResetFill();
        }

        if (uiPanel != null)
        {
            uiPanel.SetActive(true);
        }

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
        _cutInObject.SetActive(true);

        yield return new WaitForSeconds(_cutInDisplayTime);

        _cutInObject.SetActive(false);
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

}

