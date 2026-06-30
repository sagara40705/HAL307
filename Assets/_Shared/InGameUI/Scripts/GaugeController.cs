using UnityEngine;
using UnityEngine.UI;
using System.Collections;
//using FeverCutIn;


public class GaugeController : MonoBehaviour
{
    [SerializeField] private Image gaugeImage;
    [SerializeField] private float maxPoints = 10f;
    private float currentPoints = 0f; // 現在のポイント

    [Header("ゲージマックス時のアニメーション")]
    [SerializeField] private GameObject fireAnimObject;
    [SerializeField] private float maxDuration = 3.0f;

    [SerializeField] private Image CutInImage;

    private readonly string fillAmountProperty = "_FillAmount";
    private bool isMaxActive = false;
    private float maxTimer = 0f;
    

    // ゲージを増やす
    public void AddPoints(int amount)
    {
        currentPoints += amount;
        float fillRate = currentPoints / maxPoints; //

        gaugeImage.fillAmount = fillRate;

        if (gaugeImage.material != null)
        {
            gaugeImage.material.SetFloat(fillAmountProperty, fillRate);
        }

        if (currentPoints > maxPoints)
        {
            // ゲージが満タンになったときの処理
            StartMaxGauge();
        }

    }

    public void ResetGauge()
    {
        currentPoints = 0f;
        gaugeImage.fillAmount = 0f;

        if (gaugeImage.material != null)
        {
            gaugeImage.material.SetFloat(fillAmountProperty, 0f);
        }

    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ResetGauge();
        if (fireAnimObject != null)
        {
            fireAnimObject.SetActive(false);
        }

        if (CutInImage != null)
        { 
            CutInImage.gameObject.SetActive(false);
        }
    }

    // Update is called once per frame
    void Update()
    {
        // ゲージがマックスの時だけ、毎フレーム時間を進める
        if (isMaxActive)
        {
            maxTimer += Time.deltaTime;

            // 指定した時間（3秒）が経ったら終了処理を呼ぶ
            if (maxTimer >= maxDuration)
            {
                EndMaxGauge();
            }
        }
    }

    // マックス演出を開始するメソッド
    private void StartMaxGauge()
    {
        StartCoroutine(PlayFeverCutIn());
        isMaxActive = true;
        maxTimer = 0f; // タイマーをリセット
        currentPoints = maxPoints;
        UpdateGaugeUI(1f);

        if (fireAnimObject != null)
        {
            fireAnimObject.SetActive(true); // 炎を表示（Animatorが動き出す）
        }
    }

    // マックス演出を終了するメソッド
    private void EndMaxGauge()
    {
        isMaxActive = false;

        if (fireAnimObject != null)
        {
            fireAnimObject.SetActive(false); // 炎を非表示
        }

        ResetGauge();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.OnGaugeFull();
        }
    }

    private void UpdateGaugeUI(float fillRate)
    {
        if (gaugeImage != null)
        {
            gaugeImage.fillAmount = fillRate;

            if (gaugeImage.material != null)
            {
                gaugeImage.material.SetFloat(fillAmountProperty, fillRate);
            }
        }
    }

    public IEnumerator PlayFeverCutIn()
    {
        CutInImage.gameObject.SetActive(true);
        yield return new WaitForSeconds(3.0f);
        CutInImage.gameObject.SetActive(false);
    }
}
