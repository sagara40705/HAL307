using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 共通UI管理（RootUIManager）
/// </summary>
public sealed class RootUIManager : AppSingletonMono<RootUIManager>
{
    private RootUIRefs _refs;
    private Coroutine _initCoroutine;

    /// <summary>RootUIRefs を取得できて利用可能になったら true。</summary>
    public bool IsReady;

    /// <summary> 共通UIが配置されているScene </summary>
    private const string RootUISceneName = "RootUI";

    /// <summary> Bootから一度だけ呼ぶ初期化 / RootUIシーンのAdditiveロード → RootUIRefs取得 → 初期状態セット </summary>
    public void Initialize()
    {
        // 既にReadyなら何もしない
        if (IsReady) return;
        // 既に初期化中なら二重起動しない
        if (_initCoroutine != null) return;

        // 初期化開始
        HasFailed = false;
        LastError = null;
        IsReady = false;
        _initCoroutine = StartCoroutine(InitializeRoutine());
    }

    /// <summary> 初期化処理本体 </summary>
    private IEnumerator InitializeRoutine()
    {
        // 0) すでにDontDestroyOnLoad側に存在する場合（再生開始シーンが違う等）
        _refs = FindObjectOfType<RootUIRefs>(includeInactive: true);
        if (_refs != null)
        {
            // 既に常駐済みを利用
            _initCoroutine = null;
            yield break;
        }


        // 1) RootUIシーンをAdditiveロード
        if (!IsSceneLoaded(RootUISceneName))
        {
            var op = SceneManager.LoadSceneAsync(RootUISceneName, LoadSceneMode.Additive);
            if (op == null)
            {
                Fail($"LoadSceneAsync failed. Scene '{RootUISceneName}' not in Build Settings or name mismatch.");
                yield break;
            } // 例外判定

            while (!op.isDone) yield return null;
        }
        else
        {
            Fail($"Scene '{RootUISceneName}' is already loaded. RootUIManager requires exclusive control over this scene.");
            yield break;
        } // 例外判定


        // 2) RootUIシーン内から RootUIRefs を探す
        var sc = SceneManager.GetSceneByName(RootUISceneName);
        if (!sc.IsValid() || !sc.isLoaded)
        {
            Fail($"Scene '{RootUISceneName}' is not loaded after request.");
            yield break;
        } // 例外判定

        _refs = FindObjectOfType<RootUIRefs>(includeInactive: true);
        if (_refs == null)
        {
            Fail($"RootUIRefs not found in scene '{RootUISceneName}'. Put exactly one RootUIRefs in RootUI scene.");
            yield break;
        } // 例外判定


        // 3) 常駐化（UIRootCanvasを指定）
        GameObject persistRoot = _refs.PersistRoot != null? _refs.PersistRoot : _refs.transform.root.gameObject;
        DontDestroyOnLoad(persistRoot);


        // 4) RootUIシーンは空にできるのでアンロード（UIはDontDestroyOnLoadへ移動済み）
        var uop = SceneManager.UnloadSceneAsync(sc);
        // アンロード完了待ちは必須ではないが、安定させたいなら待つ
        while (uop != null && !uop.isDone) yield return null;


        // 5) 初期状態
        _refs.FadeView?.HideImmediate();
        _refs.LoadingView?.Hide();

        // 完了
        _initCoroutine = null;
        IsReady = true;
    }


    // ========== 公開API ==========

    /// <summary> フェードアウト </summary>
    public void FadeOut(float seconds) => _refs?.FadeView?.FadeOut(seconds);
    /// <summary> フェードイン </summary>
    public void FadeIn(float seconds) => _refs?.FadeView?.FadeIn(seconds);

    /// <summary>初期化処理が走っているか。</summary>
    public bool IsInitializing => _initCoroutine != null;

    /// <summary>初期化に失敗したか（デバッグ用）。</summary>
    public bool HasFailed { get; private set; }

    /// <summary>失敗理由（デバッグ用）。</summary>
    public string LastError { get; private set; }


    /// ========== 内部ヘルパー ==========

    /// <summary> 初期化失敗時の共通処理 </summary>
    private void Fail(string message)
    {
        HasFailed = true;
        LastError = message;
        Debug.LogError($"[RootUIManager] {message}");

        _refs = null;
        _initCoroutine = null;
        IsReady = false;
    }

    /// <summary> 指定したSceneがロード済みかどうか </summary>
    private static bool IsSceneLoaded(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            if (SceneManager.GetSceneAt(i).name == sceneName) return true;
        }
        return false;
    }

    /// <summary> 指定したScene内から RootUIRefs を探す </summary>
    private static RootUIRefs FindRefsInScene(Scene scene)
    {
        var roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            var found = roots[i].GetComponentInChildren<RootUIRefs>(true);
            if (found != null) return found;
        }
        return null;
    }
}


