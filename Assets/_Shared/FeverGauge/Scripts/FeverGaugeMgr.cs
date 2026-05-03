using System;
using UnityEngine;

namespace FeverGauge.Sandbox
{
    // <summary>
    // フィーバーゲージの管理クラス(ゲーム内のロジックを担当)
    // </summary>
    public class FeverGaugeMgr : MonoBehaviour
    {
        [Header("Gauge")]
        [SerializeField] private int maxGauge = 100;    // ゲージの最大値
        [SerializeField] private int initialGauge = 0;  // ゲージの初期値

        [Header("Fever")]
        [SerializeField] private float feverDuration = 10f; // フィーバーモードの持続時間（秒）

        private int currentGauge;   // 現在のゲージ値
        private bool isFeverMode;   // フィーバーモード中かどうか
        private float feverTimer;   // フィーバーモードの残り時間


        // 外部参照プロパティ

        // ゲージの最大値
        public int MaxGauge => maxGauge;

        // 現在のゲージ値
        public int CurrentGauge => currentGauge;

        // フィーバーモード中かどうか
        public bool IsFeverMode => isFeverMode;

        // フィーバーモードの持続時間
        public float FeverDuration => feverDuration;

        // フィーバーモードの残り時間
        public float FeverRemainingTime => feverTimer;

        // フィーバーモードの経過時間
        public float FeverElapsedTime => isFeverMode ? feverDuration - feverTimer : 0f;

        // ゲージの割合（0.0～1.0）
        public float GaugeRate => maxGauge <= 0 ? 0f : (float)currentGauge / maxGauge;


        // UI用のイベント
        public event Action<int, int> OnGaugeChanged;   // (currentGauge, maxGauge)の形式でゲージの変化を通知
        public event Action OnFeverStarted;             // フィーバーモード開始を通知
        public event Action OnFeverEnded;               // フィーバーモード終了を通知

        private void Awake()
        {
            Initialize();
        }

        private void Update()
        {
            if (!isFeverMode)
            {
                return;
            }

            feverTimer -= Time.deltaTime;
            if (feverTimer <= 0f)
            {
                EndFeverMode();
            }
        }

        private void Initialize()
        {
            maxGauge = Mathf.Max(1, maxGauge);
            feverDuration = Mathf.Max(0.1f, feverDuration);

            currentGauge = Mathf.Clamp(initialGauge, 0, maxGauge);
            isFeverMode = false;
            feverTimer = 0f;

            OnGaugeChanged?.Invoke(currentGauge, maxGauge);
        }


        // 外部から呼び出すメソッド
        public void AddGauge(int amount)
        {
            if (isFeverMode) { return; }

            if (amount <= 0) { return; }

            currentGauge = Mathf.Clamp(currentGauge + amount, 0, maxGauge);
            OnGaugeChanged?.Invoke(currentGauge, maxGauge);

            if (currentGauge >= maxGauge)
            {
                StartFeverMode();
            }
        }

        public void SetGauge(int amount)
        {
            if (isFeverMode) { return; }

            currentGauge = Mathf.Clamp(amount, 0, maxGauge);
            OnGaugeChanged?.Invoke(currentGauge, maxGauge);

            if (currentGauge >= maxGauge)
            {
                StartFeverMode();
            }
        }


        // 内部処理用のメソッド
        private void StartFeverMode()
        {
            if (isFeverMode) { return; }

            isFeverMode = true;
            feverTimer = feverDuration;
            currentGauge = maxGauge;

            OnGaugeChanged?.Invoke(currentGauge, maxGauge);
            OnFeverStarted?.Invoke();

            Debug.Log("フィーバーモード開始");
        }

        private bool EndFeverMode()
        {
            if (!isFeverMode) { return false; }

            isFeverMode = false;
            feverTimer = 0f;
            currentGauge = 0;

            OnGaugeChanged?.Invoke(currentGauge, maxGauge);
            OnFeverEnded?.Invoke();

            Debug.Log("フィーバーモード終了");
            return true;
        }

        // デバッグ用のメソッド
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        public void DebugStartFeverMode()
        {
            Debug.Log("Debug: フィーバーモード開始");
            StartFeverMode();
        }

        public void DebugEndFeverMode()
        {
            if (EndFeverMode())
            {
                Debug.Log("Debug: フィーバーモード終了");
            }
            else
            {
                Debug.Log("Debug: フィーバーモード中ではありません");
            }
        }
#endif
    }

}


