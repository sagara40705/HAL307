using System.Collections;
using System.Collections.Generic;
//using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// SceneLoader（非同期 / フェード無し）
///
/// 目的:
/// ・シーン遷移を非同期化してフリーズを減らす
/// ・呼び出し側を変えずに、将来フェード/ロード画面に拡張できる
///
/// 方針:
/// ・AppSingletonMonoでBootから生成（EnsureCreatedFromBoot）
/// ・同時ロードを禁止（連打事故を防ぐ）
/// ・progress(0〜1) を公開して、UIに繋げられるようにする
/// ・activateOnLoaded=false にすれば「0.9まで進めてから切替」もできる（今は即切替）
/// </summary>
public sealed class SceneLoader : AppSingletonMono<SceneLoader>
{
    /// <========= プロパティ ========== >////

    /// <summary>ロード中かどうか。</summary>
    public bool IsLoading { get; private set; }

    /// <summary>最後に開始したロードの進捗（0〜1）。</summary>
    public float Progress { get; private set; }

    /// <summary>最後に要求されたシーン名。</summary>
    public string TargetSceneName { get; private set; }

    /// <summary> ロード中のコルーチン </summary>
    private Coroutine _loadingCoroutine;

    /// <summary> ロード中のAsyncOperation参照 </summary>
    private AsyncOperation _currentOp;

    /// <summary> フェードアウト時間（秒） </summary>
    private const float FadeOutSeconds = 1.25f;
    /// <summary> フェードイン時間（秒） </summary>
    private const float FadeInSeconds = 1.25f;


    ///< ========== 公開API ========== >////

    /// <summary> 単純にシーンを切り替える（同期ロード）</summary>
    public void Load(string sceneName)
    {
        // 終了中などで Instance が null の可能性はあるが、
        // このメソッドは Instance 経由で呼ぶ想定なので、ここではガードを最小にする。
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[SceneLoader] sceneName is null/empty.");
            return;
        }

        // すでに同じシーンなら何もしない（事故防止）
        string current = SceneManager.GetActiveScene().name;
        if (current == sceneName)
            return;

        SceneManager.LoadScene(sceneName);
    }

    /// <summary> BuildIndexで切り替え（シーン名のタイポを避けたい場合用）</summary>
    public void Load(int buildIndex)
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        if (currentIndex == buildIndex)
            return;

        SceneManager.LoadScene(buildIndex);
    }

    /// <summary> いまのシーンをリロード </summary>
    public void Reload()
    {
        var scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name);
    }

    /// <summary> アプリ終了（Editorでは再生停止）</summary>
    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary> 非同期でシーンを読み込み、完了したら切り替える（Single）/ すでにロード中なら無視する（連打対策）</summary>
    public void LoadAsync(string sceneName, bool waitForActivation = false)
    {
        // すでにロード中なら無視
        if (IsLoading) return; 

        // 引数チェック
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[SceneLoader] sceneName is null/empty.");
            return;
        }

        // すでに同じシーンなら何もしない（事故防止）
        if (SceneManager.GetActiveScene().name == sceneName)
        {
            Debug.LogWarning("[SceneLoader] Target scene is the same as current scene. Ignored.");
            return;
        }

        // ロード開始
        TargetSceneName = sceneName;
        _loadingCoroutine = StartCoroutine(LoadAsyncRoutine(sceneName, waitForActivation));
    }

    /// <summary> 非同期ロード中なら中断する（必要になったら使う）/ ※LoadSceneAsync自体のキャンセルは基本できないので、ここでは表示状態の整合などのための停止 </summary>
    public void StopLoading()
    {
        if (_loadingCoroutine != null)
        {
            StopCoroutine(_loadingCoroutine);
            _loadingCoroutine = null;
            Debug.Log("[SceneLoader] Loading coroutine stopped.");
        }

        IsLoading = false;
        Progress = 0f;
        TargetSceneName = null;
    }

    /// <summary> 切替許可API / returns: 切替要求を出せたかどうか（ロード中でなければ false）</summary>
    public bool ActivateLoadedScene()
    {
        if (_currentOp != null)
        {
            _currentOp.allowSceneActivation = true;
            return true;
        }

        return false;
    }

    /// <========= 内部処理 ========== >////

    /// <summary> asyncロード処理本体 </summary>
    private IEnumerator LoadAsyncRoutine(string sceneName, bool waitForActivation)
    {
        // ロード開始状態セット
        IsLoading = true;
        Progress = 0f;

        // 2) フェードアウト（黒くする）
        RootUIManager.Instance.FadeOut(FadeOutSeconds);
        // フェードアウト中は待機
        yield return new WaitForSecondsRealtime(FadeOutSeconds);

        // 3) 非同期ロード開始（アクティベーションは制御）
        _currentOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        // nullの可能性は低いが保険
        if (_currentOp == null)
        {
            Debug.LogError($"[SceneLoader] LoadSceneAsync returned null. sceneName={sceneName}");
            IsLoading = false;
            yield break;
        }
        // シーン切替を即座に行うかどうか
        _currentOp.allowSceneActivation = !waitForActivation;

        // 4) ロード進捗更新
        // UnityのAsyncOperation.progress は 0〜0.9 までがロード、0.9到達後にアクティベートで完了という挙動
        // allowSceneActivation=true の場合は、実質すぐ完了するので 1.0 まで流れる想定
        while (!_currentOp.isDone)
        {
            // progressは最大0.9なので、UI向けに0〜1に正規化しておく
            Progress = Mathf.Clamp01(_currentOp.progress / 0.9f);

            // waitForActivation=true だと 0.9 到達後はここで止まる
            yield return null;
        }
        Progress = 1f; // 念のため完了時は1.0にしておく

        // 5) アクティベート後（isDoneになった後）にフェードイン
        RootUIManager.Instance.FadeIn(FadeInSeconds);
        yield return new WaitForSecondsRealtime(FadeInSeconds);

        // 6) クリーンアップ(終了)
        CreanUp();
    }

    /// <summary> プロパティの初期化 </summary>
    private void CreanUp()
    {
        IsLoading = false;
        _loadingCoroutine = null;
        _currentOp = null;
        TargetSceneName = null;
    }
}
