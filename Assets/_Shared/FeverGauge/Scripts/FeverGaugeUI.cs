using UnityEngine;
using UnityEngine.UI;

namespace FeverGauge.Sandbox
{
    public class FeverGaugeUI : MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField] private FeverGaugeMgr feverGaugeMgr;

        [Header("Gauge UI")]
        [SerializeField] private Slider gaugeSlider;

        [Header("Fever UI")]
        [SerializeField] private GameObject feverLabel;

        private void OnEnable()
        {
            if (feverGaugeMgr == null)
            {
                Debug.LogWarning("FeverGaugeMgr が設定されていません。", this);
                return;
            }

            feverGaugeMgr.OnGaugeChanged += UpdateGaugeUI;
            feverGaugeMgr.OnFeverStarted += ShowFeverUI;
            feverGaugeMgr.OnFeverEnded += HideFeverUI;

            Refresh();
        }

        private void OnDisable()
        {
            if (feverGaugeMgr == null)
            {
                return;
            }

            feverGaugeMgr.OnGaugeChanged -= UpdateGaugeUI;
            feverGaugeMgr.OnFeverStarted -= ShowFeverUI;
            feverGaugeMgr.OnFeverEnded -= HideFeverUI;
        }

        private void Update()
        {
            UpdateFeverTimeUI();
        }

        // UIのリフレッシュ
        private void Refresh()
        {
            UpdateGaugeUI(feverGaugeMgr.CurrentGauge, feverGaugeMgr.MaxGauge);

            if (feverGaugeMgr.IsFeverMode)
            {
                ShowFeverUI();
            }
            else
            {
                HideFeverUI();
            }

            UpdateFeverTimeUI();
        }

        // ゲージUIの更新
        private void UpdateGaugeUI(int currentGauge, int maxGauge)
        {
            if (gaugeSlider != null)
            {
                gaugeSlider.maxValue = maxGauge;
                gaugeSlider.value = currentGauge;
            }
        }

        // フィーバーUIの表示
        private void ShowFeverUI()
        {
            if (feverLabel != null)
            {
                feverLabel.SetActive(true);
            }
        }

        // フィーバーUIの非表示
        private void HideFeverUI()
        {
            if (feverLabel != null)
            {
                feverLabel.SetActive(false);
            }

        }


        private void UpdateFeverTimeUI()
        {
            if (feverGaugeMgr == null)
            {
                return;
            }


            if (!feverGaugeMgr.IsFeverMode)
            {
                return;
            }
        }

    }
}
