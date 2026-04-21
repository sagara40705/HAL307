using UnityEngine;

//Debugで動かしてよい対象のマーカー
[DisallowMultipleComponent]
public sealed class DebugMovable : MonoBehaviour 
{
    [SerializeField] private bool allowPosition = true;
    [SerializeField] private bool allowRotation = false;
    [SerializeField] private bool allowScale = false;

    private Vector3 _initialPosition;
    private Quaternion _initialRotation;
    private Vector3 _initialScale;

    public bool AllowPosition => allowPosition;
    public bool AllowRotation => allowRotation;
    public bool AllowScale => allowScale;

    public Transform TargetTransform => transform;

    private void Awake()
    {
        CaptureInitial();
    }

    public void CaptureInitial()
    {
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;
        _initialScale = transform.localScale;
    }

    public void ResetToInitial()
    {
        transform.position = _initialPosition;
        transform.rotation = _initialRotation;
        transform.localScale = _initialScale;
    }
}
