using System.Collections;
using UnityEngine;

public class AdjustableUiPresenter : MonoBehaviour
{
    [Header("対象")]
    [SerializeField] private RectTransform _targetRect;
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("初期状態")]
    [SerializeField] private bool _hideOnAwake = true;
    [SerializeField] private bool _deactivateWhenHidden = true;

    [Header("表示位置")]
    [SerializeField] private Vector2 _positionOffset = Vector2.zero;

    [Header("表示演出")]
    [SerializeField] private float _showDelay = 0f;
    [SerializeField] private float _fadeInDuration = 0.2f;
    [SerializeField] private Vector3 _hiddenScale = new Vector3(0.85f, 0.85f, 0.85f);
    [SerializeField] private Vector3 _shownScale = Vector3.one;

    [Header("自動非表示")]
    [SerializeField] private bool _autoHide = false;
    [SerializeField] private float _displayDuration = 2f;

    [Header("非表示演出")]
    [SerializeField] private float _fadeOutDuration = 0.2f;
    [SerializeField] private Vector3 _hideEndScale = new Vector3(1.15f, 1.15f, 1.15f);

    [Header("時間設定")]
    [SerializeField] private bool _useUnscaledTime = true;

    private Vector2 _baseAnchoredPosition;
    private Coroutine _routine;

    // 
    private void Awake()
    {
        SetupReferences();
        CaptureCurrentAsBase();

        if (_hideOnAwake)
        {
            ApplyHiddenImmediate();
        }
    }

    private void SetupReferences()
    {
        if (_targetRect == null)
        {
            _targetRect = GetComponent<RectTransform>();
        }

        if (_canvasGroup == null)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public void CaptureCurrentAsBase()
    {
        SetupReferences();

        if (_targetRect != null)
        {
            _baseAnchoredPosition = _targetRect.anchoredPosition;
        }
    }

    public void Show()
    {
        SetupReferences();

        gameObject.SetActive(true);

        if (_routine != null)
        {
            StopCoroutine(_routine);
        }

        _routine = StartCoroutine(ShowRoutine());
    }

    public void Hide()
    {
        SetupReferences();

        if (_routine != null)
        {
            StopCoroutine(_routine);
        }

        _routine = StartCoroutine(HideRoutine());
    }

    public void ShowImmediate()
    {
        SetupReferences();

        gameObject.SetActive(true);

        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        ApplyShownImmediate();
    }

    public void HideImmediate()
    {
        SetupReferences();

        if (_routine != null)
        {
            StopCoroutine(_routine);
            _routine = null;
        }

        ApplyHiddenImmediate();
    }

    private IEnumerator ShowRoutine()
    {
        if (_showDelay > 0f)
        {
            yield return Wait(_showDelay);
        }

        if (_targetRect != null)
        {
            _targetRect.anchoredPosition = _baseAnchoredPosition + _positionOffset;
        }

        float timer = 0f;
        float duration = Mathf.Max(0.0001f, _fadeInDuration);

        while (timer < duration)
        {
            timer += DeltaTime();

            float t = Mathf.Clamp01(timer / duration);
            float eased = EaseOutBack(t);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = t;
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.interactable = true;
            }

            transform.localScale = Vector3.LerpUnclamped(_hiddenScale, _shownScale, eased);

            yield return null;
        }

        ApplyShownImmediate();

        if (_autoHide)
        {
            yield return Wait(_displayDuration);
            yield return HideRoutine();
        }

        _routine = null;
    }

    private IEnumerator HideRoutine()
    {
        float timer = 0f;
        float duration = Mathf.Max(0.0001f, _fadeOutDuration);

        Vector3 startScale = transform.localScale;

        while (timer < duration)
        {
            timer += DeltaTime();

            float t = Mathf.Clamp01(timer / duration);

            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = 1f - t;
                _canvasGroup.blocksRaycasts = false;
                _canvasGroup.interactable = false;
            }

            transform.localScale = Vector3.Lerp(startScale, _hideEndScale, t);

            yield return null;
        }

        ApplyHiddenImmediate();

        _routine = null;
    }

    private void ApplyShownImmediate()
    {
        if (_targetRect != null)
        {
            _targetRect.anchoredPosition = _baseAnchoredPosition + _positionOffset;
        }

        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 1f;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.interactable = true;
        }

        transform.localScale = _shownScale;
    }

    private void ApplyHiddenImmediate()
    {
        if (_canvasGroup != null)
        {
            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;
        }

        transform.localScale = _hiddenScale;

        if (_deactivateWhenHidden)
        {
            gameObject.SetActive(false);
        }
    }

    private float DeltaTime()
    {
        return _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private WaitForSecondsRealtime Wait(float seconds)
    {
        return new WaitForSecondsRealtime(Mathf.Max(0f, seconds));
    }

    private static float EaseOutBack(float t)
    {
        float c1 = 1.70158f;
        float c3 = c1 + 1f;

        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
