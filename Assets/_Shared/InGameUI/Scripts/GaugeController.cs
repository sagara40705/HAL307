using UnityEngine;
using UnityEngine.UI;

public class GaugeController : MonoBehaviour
{
    [SerializeField] private Image gaugeImage;
    [SerializeField] private float maxPoints = 10f;
    private float currentPoints = 0f; // 現在のポイント

    // ゲージを増やす
    public void AddPoints(int amount)
    {
        currentPoints += amount;
        gaugeImage.fillAmount = currentPoints / maxPoints; //

        if(currentPoints > maxPoints)
        {
            // ゲージが満タンになったときの処理
            currentPoints -= maxPoints;
            UIManager.Instance.OnGaugeFull();
        }

    }

    public void ResetGauge()
    {
        currentPoints = 0f;
        gaugeImage.fillAmount = 0f;
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
