using UnityEngine;
using UnityEngine.UI;

public class WaveGaugeController : MonoBehaviour
{
    [SerializeField] private Material waveMaterial;
    [SerializeField] private float decreaseSpeed = 0.1f;
    [SerializeField] private string propertyName = "_BaseLine";
    [SerializeField] private float maxValue = 1.0f;
    [SerializeField] private float minValue = 0.0f;

    [SerializeField] private Image warningImage;
    [SerializeField] private float warningThreshold = 0.2f; //20%
    [SerializeField] private float blinkSpeed = 5f;

    private float currentValue;

    void Start()
    {
        // インスペクターでマテリアルが設定されていない場合、アタッチされているImageから取得を試みる
        if (waveMaterial == null)
        {
            Image image = GetComponent<Image>();
            if (image != null)
            {
                // 実行時にマテリアルのインスタンスを作成して操作する
                waveMaterial = image.material;
            }
        }

        // マテリアルが存在するかチェック
        if (waveMaterial != null)
        {
            //満タンの値をセットしてスタートする
            currentValue = maxValue;
            waveMaterial.SetFloat(propertyName, currentValue);
        }
        else
        {
            Debug.LogError("WaveMaterialが見つかりません。");
        }
    }

    void Update()
    {
        // マテリアルがあり、値が最小値より大きい場合のみ減少させる
        if (waveMaterial != null && currentValue > minValue)
        {
            // 時間経過に合わせて値を減らす
            currentValue -= decreaseSpeed * Time.deltaTime;

            // 値が最小値を下回らないように制限
            currentValue = Mathf.Max(minValue, currentValue);

            // シェーダーに新しい値をセット
            waveMaterial.SetFloat(propertyName, currentValue);
        }

        // 残量が少なくなったら点滅
        if (warningImage != null)
        {
            if (currentValue <= warningThreshold)
            {
                Color color = warningImage.color;
                color.a = Mathf.PingPong(Time.time * blinkSpeed, 1f);
                warningImage.color = color;
            }
            else
            {
                Color color = warningImage.color;
                color.a = 0f;
                warningImage.color = color;
            }
        }
    }
}
