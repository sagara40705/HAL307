using UnityEngine;

public class StopLight : MonoBehaviour
{
    [SerializeField] private Light targetLight;
    [SerializeField] private bool turnOffOnStart = true;

    public bool IsOn => targetLight != null && targetLight.enabled;

    private void Awake()
    {
        SetupTargetLight();
    }

    private void Start()
    {
        if (turnOffOnStart)
        {
            SetLight(false);
        }
    }

    public void SetLight(bool isOn)
    {
        SetupTargetLight();
        if (targetLight == null)
        {
            Debug.LogWarning("[StopLight] Target Light was not found.", this);
            return;
        }

        targetLight.enabled = isOn;
    }

    public void TurnOn()
    {
        SetLight(true);
    }

    public void TurnOff()
    {
        SetLight(false);
    }

    private void SetupTargetLight()
    {
        if (targetLight != null)
        {
            return;
        }

        targetLight = GetComponentInChildren<Light>(true);
    }
}
