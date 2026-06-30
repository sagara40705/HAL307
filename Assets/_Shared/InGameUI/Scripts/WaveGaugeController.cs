using UnityEngine;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class WaveGaugeController : MonoBehaviour
{
    [SerializeField] private Material waveMaterial;
    [SerializeField] private float decreaseSpeed = 0.1f;
    [SerializeField] private string propertyName = "_BaseLine";
    [SerializeField] private float maxValue = 1.0f;
    [SerializeField] private float minValue = 0.0f;

    [Header("警告UI")]
    [SerializeField] private GameObject warningUI;
    [SerializeField] private float warningThreshold = 0.2f;

    private float currentValue;

    // WaveGaugeが0になった時に何度も演出が出ないようにするフラグ
    private bool isTransitionStarted = false;

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
            // 満タンの値をセットしてスタートする
            currentValue = maxValue;
            waveMaterial.SetFloat(propertyName, currentValue);
        }
        else
        {
            Debug.LogError("WaveMaterialが見つかりません。");
        }

        // 最初は警告UIを非表示
        if (warningUI != null)
        {
            warningUI.SetActive(false);
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

            // ゲージが少なくなったら警告UIを表示
            if (warningUI != null)
            {
                warningUI.SetActive(currentValue <= warningThreshold);
            }

            // WaveGaugeが0になったらシーン遷移演出を開始
            if (!isTransitionStarted && currentValue <= minValue)
            {
                isTransitionStarted = true;
                PlayGaugeEmptyTransition().Forget();
            }
        }
    }

    private async UniTaskVoid PlayGaugeEmptyTransition()
    {
        await TransitionManager.PlayDumbbellFadeOut();
    }
}
