using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public abstract class AppSingletonMono<T> : MonoBehaviour where T : MonoBehaviour
{
    /// ==================================
    /// --- 静的（アプリ全体で唯一）状態 ---
    /// ==================================

    /// <summary>唯一のインスタンス参照</summary>
    private static T s_instance;

    /// <summary>アプリ終了処理に入ったか</summary>
    private static bool s_isQuitting;

    /// <summary>生成している最中かどうか</summary>
    private static bool s_isBootCreating;

    /// ==================================
    /// --- 公開API ---
    /// ==================================

    /// <summary>
    /// 現在のSingleton参照。
    /// 自動生成はしない。未生成なら null。
    /// 終了中（OnApplicationQuit後）は null を返す。
    /// </summary>
    public static T Instance => s_isQuitting ? null : s_instance;

    /// <summary>生成済みかどうか（終了中は false 扱い）。</summary>
    public static bool HasInstance => !s_isQuitting && s_instance != null;


    /// <summary>
    /// BootSceneからのみ呼ぶ生成関数。
    /// ・Boot以外で呼ばれたらエラー（生成しない）
    /// ・既に存在すればそれを返す
    /// ・存在しなければ GameObject を生成して AddComponent する（Awakeが即呼ばれる）
    /// </summary>
    public static T EnsureCreatedFromBoot()
    {
        // 終了中は何もしない（終了時に生成が走ると不具合の元）
        if (s_isQuitting) return null;

        // 既に生成済みならそれを返す
        if (s_instance != null) return s_instance;

        // 現在のアクティブシーンがBootかチェック
        string activeSceneName = SceneManager.GetActiveScene().name;
        if (activeSceneName != BootConfig.BootSceneName)
        {
            Debug.LogError(
                $"[AppSingleton] {typeof(T).Name} must be created in Boot scene only. " +
                $"ActiveScene='{activeSceneName}', BootScene='{BootConfig.BootSceneName}'."
            );
            return null;
        }

        // 既にBootシーン上に配置されているケースを許容したい場合は探す
        // ※「Bootstrapperだけが生成する」運用なら、基本は見つからないはず
        s_instance = FindObjectOfType<T>();
        if (s_instance != null)
        {
            // 配置されていたものを採用（Awake内でもBoot配置として許可される）
            return s_instance;
        }

        // AddComponent で生成する瞬間だけ、Awakeが許可判定できるようフラグを立てる
        s_isBootCreating = true;
        try
        {
            var go = new GameObject(typeof(T).Name);

            // ここで Awake が呼ばれる点に注意（Unityの仕様）
            s_instance = go.AddComponent<T>();

            // 常駐化
            DontDestroyOnLoad(go);
        }
        finally
        {
            s_isBootCreating = false;
        }

        return s_instance;
    }

     /// <summary>
    /// 自動生成なしで参照を取りたい場合用。
    /// ・既に生成済みなら返す
    /// ・未生成なら false（検索もしない）
    /// </summary>
    public static bool TryGet(out T instance)
    {
        if (s_isQuitting)
        {
            instance = null;
            return false;
        }

        instance = s_instance;
        return instance != null;
    }

    /// ==================================
    /// --- Unity lifecycle 
    /// ==================================

        /// <summary>
    /// 生成タイミング判定と、唯一インスタンス登録。
    /// Boot以外で勝手に生成/配置された場合はエラーで破棄。
    /// </summary>
    protected virtual void Awake()
    {
        // 終了中に生成されるのは危険なので即破棄
        if (s_isQuitting)
        {
            Destroy(gameObject);
            return;
        }

        // 既に別インスタンスがいる → 重複なので破棄
        if (s_instance != null && s_instance != this)
        {
            Debug.LogError($"[AppSingleton] Duplicate instance detected: {typeof(T).Name}. Destroying the new one.");
            Destroy(gameObject);
            return;
        }

        // このオブジェクトが生成されたシーン名（DontDestroyOnLoad移動後は特別シーンになる前なので Awake時点で確認）
        string createdInScene = gameObject.scene.name;

        // 許可条件:
        // 1) Bootから EnsureCreatedFromBoot() の AddComponent 中（s_isBootCreating == true）
        // 2) Bootシーンに最初から配置されていた（createdInScene == BootSceneName）
        bool allowed =
            s_isBootCreating ||
            createdInScene == BootConfig.BootSceneName;

        if (!allowed)
        {
            // Boot以外に置かれた、またはどこかで勝手に Instantiate された
            Debug.LogError(
                $"[AppSingleton] {typeof(T).Name} was created outside Boot. " +
                $"CreatedInScene='{createdInScene}', BootScene='{BootConfig.BootSceneName}'. " +
                $"This instance will be destroyed."
            );
            Destroy(gameObject);
            return;
        }

        // ここまで来たら正規ルートでの生成
        s_instance = this as T;

        // 常駐化（Bootに配置していた場合も、ここで常駐に移動させる）
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// アプリ終了通知。以後 Instance は null を返し、生成もしない。
    /// </summary>
    protected virtual void OnApplicationQuit()
    {
        s_isQuitting = true;
    }

    /// <summary>
    /// 破壊禁止（終了中以外で Destroy されたらエラー）。
    /// ただし、Unityの終了処理では破棄が走るため、終了中は許容する。
    /// </summary>
    protected virtual void OnDestroy()
    {
        // 終了中は通常の破棄なので何もしない
        if (s_isQuitting) return;

        // 正規インスタンスが壊された = 想定外（破壊禁止）
        if (s_instance == this)
        {
            // 参照を外す（以後 Instance は null になる）
            s_instance = null;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // 開発中は即止めた方が原因が早く潰せる（壊れて復旧するとバグが隠れる）
            throw new Exception($"[AppSingleton] Destroyed unexpectedly: {typeof(T).Name} (must live until quit).");
#else
            // リリースではクラッシュさせずログに留める（方針に応じて変更可）
            Debug.LogError($"[AppSingleton] Destroyed unexpectedly: {typeof(T).Name} (must live until quit).");
#endif
        }
    }
}

/// <summary>
/// Bootシーン名を一箇所で管理するための定数クラス。
/// </summary>
public static class BootConfig
{
    public const string BootSceneName = "Boot";
}
