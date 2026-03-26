using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// アプリ起動から終了まで常駐する Singleton 基底。
/// ・生成は BootScene のみ許可
/// ・Instance 参照では自動生成しない
/// ・Boot 以外で生成 / 配置されたらエラー
/// ・プログラム終了まで破壊禁止
/// ・DontDestroyOnLoad で常駐
/// </summary>
public abstract class AppLifetimeSingleton<T> : MonoBehaviour
    where T : AppLifetimeSingleton<T>
{
    private static T s_instance;
    private static bool s_isQuitting;
    private static bool s_isBootCreating;

    /// <summary>現在のインスタンス。未生成なら null。</summary>
    public static T Instance => s_isQuitting ? null : s_instance;

    /// <summary>生成済みかどうか。</summary>
    public static bool HasInstance => !s_isQuitting && s_instance != null;

    /// <summary>自動生成なしで参照取得したい場合用。</summary>
    public static bool TryGetInstance(out T instance)
    {
        instance = Instance;
        return instance != null;
    }

    /// <summary>
    /// Boot からだけ呼ぶ生成関数。
    /// 既に存在すればそれを返す。
    /// </summary>
    public static T CreateFromBootIfNeeded()
    {
        if (s_isQuitting)
            return null;

        if (s_instance != null)
            return s_instance;

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.name != AppBootConfig.BootSceneName)
        {
            Debug.LogError(
                $"[{typeof(T).Name}] must be created only from Boot. " +
                $"ActiveScene='{activeScene.name}', BootScene='{AppBootConfig.BootSceneName}'.");
            return null;
        }

        // BootScene に事前配置されていたものを採用
        T preplacedInstance = FindPreplacedInstanceInActiveBootScene();
        if (preplacedInstance != null)
        {
            s_instance = preplacedInstance;
            return s_instance;
        }

        // Boot 経由の正規生成
        s_isBootCreating = true;
        try
        {
            GameObject go = new GameObject(typeof(T).Name);
            s_instance = go.AddComponent<T>();
        }
        finally
        {
            s_isBootCreating = false;
        }

        return s_instance;
    }

    protected virtual void Awake()
    {
        if (s_isQuitting)
        {
            Destroy(gameObject);
            return;
        }

        if (s_instance != null && !ReferenceEquals(s_instance, this))
        {
            Debug.LogError(
                $"[{typeof(T).Name}] duplicate instance detected. Destroying the newer instance.");
            Destroy(gameObject);
            return;
        }

        bool createdFromBoot = s_isBootCreating;
        bool placedInBootScene = gameObject.scene.name == AppBootConfig.BootSceneName;

        if (!createdFromBoot && !placedInBootScene)
        {
            Debug.LogError(
                $"[{typeof(T).Name}] was created outside Boot. " +
                $"CreatedInScene='{gameObject.scene.name}', BootScene='{AppBootConfig.BootSceneName}'.");
            Destroy(gameObject);
            return;
        }

        s_instance = this as T;

        // ** DontDestroyOnLoad は root に対して使う前提なので切り離す
        if (transform.parent != null)
        {
            transform.SetParent(null);
        }

        DontDestroyOnLoad(gameObject);

        OnSingletonAwake();
    }

    /// <summary>
    /// Singleton 登録と常駐化が終わった直後のフック。
    /// </summary>
    protected virtual void OnSingletonAwake()
    {
    }

    protected virtual void OnApplicationQuit()
    {
        s_isQuitting = true;
    }

    protected virtual void OnDestroy()
    {
        if (s_isQuitting)
            return;

        if (!ReferenceEquals(s_instance, this))
            return;

        s_instance = null;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        throw new Exception(
            $"[{typeof(T).Name}] was destroyed unexpectedly. It must live until application quit.");
#else
        Debug.LogError(
            $"[{typeof(T).Name}] was destroyed unexpectedly. It must live until application quit.");
#endif
    }

    private static T FindPreplacedInstanceInActiveBootScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid())
            return null;

        T found = null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T[] candidates = root.GetComponentsInChildren<T>(true);

            foreach (T candidate in candidates)
            {
                if (found != null && !ReferenceEquals(found, candidate))
                {
                    Debug.LogError(
                        $"[{typeof(T).Name}] multiple preplaced instances were found in Boot scene.");
                    return found;
                }

                found = candidate;
            }
        }

        return found;
    }
}