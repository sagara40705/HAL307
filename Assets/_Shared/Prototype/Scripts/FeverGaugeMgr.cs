using UnityEngine;

// フィーバーゲージ管理クラス
// ゲージの追加、フィーバーモードの開始と終了、ゲージの状態管理を行う
// フィーバーモード中のゲージは最大値で固定
public class FeverGaugeMgr : MonoBehaviour
{
    [SerializeField] private int _kMaxGauge = 100;  // フィーバーゲージの最大値
    [SerializeField] private int _kInitGauge = 0;   // フィーバーゲージの初期値

    [SerializeField] private float _kFeverModeDuration = 10f; // フィーバーモードの持続時間（秒）

    private int _currentGauge = 0;      // 現在のゲージ値
    private bool _feverMode = false;    // フィーバーモード中かどうか
    private float _feverModeTimer = 0f; // フィーバーモードの残り時間

    private void Start()
    {
        _currentGauge = _kInitGauge; // ゲージを初期値に設定
        _feverMode = false;
        _feverModeTimer = 0f;
    }

    private void Update()
    {
        if (_feverMode)
        {
            _feverModeTimer -= Time.deltaTime;
            if (_feverModeTimer <= 0f)
            {
                EndFeverMode();
            }
        }
    }

    public void AddGauge(int amount)
    {
        // フィーバーモード中はゲージを追加しない
        if (_feverMode) { return; }

        // ゲージを追加
        _currentGauge += amount;

        // ゲージが最大値を超えた場合はフィーバーモードを開始
        if (_currentGauge >= _kMaxGauge)
        {
            StartFeverMode();
        }
    }

    // 外部参照用関数
    public int GetMaxGauge() { return _kMaxGauge; }     // ゲージの最大値を返す関数
    public int GetCurrentGauge() { return _currentGauge; }  // 現在のゲージ値を返す関数
    public bool IsFeverMode() { return _feverMode; }    // フィーバーモード中かどうかを返す関数
    public float GetFeverModeDuration() { return _kFeverModeDuration; } // フィーバーモードの持続時間を返す関数
    public float GetFeverModeTimer() { return _feverModeTimer; } // フィーバーモードの残り時間を返す関数


    // 内部処理関数
    private void StartFeverMode()
    {
        if (_feverMode) { return; }

        Debug.Log("フィーバーモード開始！");
        _feverMode = true;
        _feverModeTimer = _kFeverModeDuration;
        _currentGauge = _kMaxGauge;
    }

    private bool EndFeverMode()
    {
        if (!_feverMode) { return false; }

        _feverMode = false;
        _feverModeTimer = 0f;
        _currentGauge = 0;
        Debug.Log("フィーバーモード終了！");
        return true;
    }

    // デバック用関数
    public void DebugStartFeverMode()
    {
        Debug.Log("Debug: フィーバーモード開始");
        StartFeverMode();
    }

    public void DebugEndFeverMode()
    {
        EndFeverMode();
        Debug.Log("Debug: フィーバーモード終了");
    }

    public void SetGauge(int amount)
    {
        if (_feverMode) { return; }

        _currentGauge = Mathf.Clamp(amount, 0, _kMaxGauge);
        Debug.Log("Debug: ゲージを設定 " + _currentGauge);

        if (_currentGauge >= _kMaxGauge)
        {
            StartFeverMode();
        }
    }

}
