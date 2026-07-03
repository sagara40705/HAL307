using System.Collections;
using AudienceRequest;
using UnityEngine;

public class FeverCutIn : MonoBehaviour
{
    [SerializeField] private FeverGaugeController feverGauge;
    [SerializeField] private GameObject cutInImage;
    [SerializeField] private float displayTime = 3f;

    private void Start()
    {
        cutInImage.SetActive(false);

        feverGauge.OnFeverStarted += ShowCutIn;
    }

    private void OnDestroy()
    {
        feverGauge.OnFeverStarted -= ShowCutIn;
    }

    private void ShowCutIn()
    {
        StartCoroutine(CutInRoutine());
    }

    private IEnumerator CutInRoutine()
    {
        cutInImage.SetActive(true);

        yield return new WaitForSeconds(displayTime);

        cutInImage.SetActive(false);
    }
}
