using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary> 
/// BootSceneに置く。
/// 常駐Appを順序固定で生成・初期化する。
/// 初期化完了後に最初のシーンを自動ロード。
///  </summary>
public sealed class AppBootstrapper : MonoBehaviour
{
    [Header("最初のScene")]
    [SerializeField] private string firstSceneNameInPlayer = "Title";
#if UNITY_EDITOR
    [SerializeField] private string firstSceneNameInEditor = "DevMenu";
#endif

    [Header("起動を待つ時間")]
    [SerializeField] private float TimeoutSeconds = 10f;


    /// <summary> 起動後に開始 </summary>
    private IEnumerator Start()
    {
        // 1) App常駐をBootで生成（順序固定）
        CreateAppSingletons();

        float timer = 0f; // タイムアウト用タイマー

        // 2) 全AppSingletonの準備完了を待つ
        while (!AreAllSingletonsReady())
        {
            timer += Time.unscaledDeltaTime; // タイムアウト用に実時間で計測
            // タイムアウト
            if (timer >= TimeoutSeconds)
            {
                Debug.LogError("[AppBootstrapper] Timeout waiting for all AppSingletons to be ready.");
                yield break;
            }

            yield return null;
        }

        // 3) 準備完了後に最初のシーンをロード 
        Debug.Log("[AppBootstrapper] Boot completed. Loading first scene...");
        LoadFirstSceneIfNeeded();
    }

    /// <summary> 順序を固定してすべての AppSingletonMono を生成する </summary>
    private void CreateAppSingletons()
    {
        // 0) デバッグコンソール（依存なし・確認用に最適）
        _ = DebugConsoleManager.EnsureCreatedFromBoot();

        // 1) シーンローダー
        _ = SceneLoader.EnsureCreatedFromBoot();

        // 2) 共通UI
        _ = RootUIManager.EnsureCreatedFromBoot();
        RootUIManager.Instance?.Initialize();

        // 3) オプション管理
        //_ = OptionManager.EnsureCreatedFromBoot();
        //OptionManager.Instance?.Initialize();

        // --- ここから先は今後追加していく枠（例） -------------------------
        // _ = SaveManager.EnsureCreatedFromBoot();     // セーブ/ロード
        // _ = InputManager.EnsureCreatedFromBoot();    // 入力
        // _ = AudioManager.EnsureCreatedFromBoot();    // 音（Option後が推奨）
    }

    /// <summary>  Boot完了後に最初のシーンへ遷移する。 </summary>
    private void LoadFirstSceneIfNeeded()
    {
        string target = GetFirstSceneName();
        if (string.IsNullOrWhiteSpace(target)) return;

        if (SceneManager.GetActiveScene().name == target)
        {
            Debug.LogWarning("[AppBootstrapper] Already in first scene. Skip.");
            return;
        }

        // ここは既存の SceneLoader 呼び出しに合わせる
        SceneLoader.Instance?.LoadAsync(target);
    }

    /// <summary> 全 AppSingleton の初期化が完了したかチェックする </summary>
    private bool AreAllSingletonsReady()
    {
        // 追加したAppSingletonがあればここでチェックを追加する
        if (!DebugConsoleManager.HasInstance) return false;
        if (!SceneLoader.HasInstance) return false;
        if (!RootUIManager.HasInstance || !RootUIManager.Instance.IsReady) return false;

        return true;
    }

    /// <summary> 最初にロードするシーン名を取得する </summary>
    private string GetFirstSceneName()
    {
#if UNITY_EDITOR
        return firstSceneNameInEditor;
#else
        return firstSceneNameInPlayer;
#endif
    }
}

