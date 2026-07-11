using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// BootScene に置く。
/// App常駐Singleton / Manager を順番に生成し、必要なものは初期化完了まで待つ。
/// Scene遷移は行わない。遷移は BootSceneRouter 側で担当する。
/// </summary>
public sealed class AppBootstrapper : MonoBehaviour
{
    [Header("各Manager初期化のタイムアウト秒")]
    [SerializeField, Min(0.1f)] private float initializeTimeoutSeconds = 10f;

    public bool IsRunning { get; private set; }
    public bool IsCompleted { get; private set; }
    public bool HasFailed { get; private set; }

    /// <summary>
    /// BootSceneRouter から呼ぶ。
    /// 完了済みなら即終了。
    /// 実行中に再度呼ばれたら完了まで待つ。
    /// </summary>
    public IEnumerator RunBootSequence()
    {
        if (IsCompleted)
            yield break;

        if (IsRunning)
        {
            while (IsRunning)
                yield return null;

            yield break;
        }

        IsRunning = true;
        HasFailed = false;

        // ------------------------------------------------------------
        // ここに起動順を書く
        // 非Manager: BootSingleton
        // Manager   : BootManager
        // ------------------------------------------------------------

        // --- 追加例 --------------------------------------------------

        // debug用のManager
        //yield return BootManager(
        //    nameof(DebugConsoleManager),
        //    () => DebugConsoleManager.CreateFromBootIfNeeded());
        //if (HasFailed)
        //{
        //    yield break;
        //}

        // ContextManager
        yield return BootManager(
            nameof(ContextManager),
            () => ContextManager.CreateFromBootIfNeeded());
        if (HasFailed)
        {
            yield break;
        }

        // RuntimeDebugManager
        yield return BootManager(
            nameof(RuntimeDebugManager),
            () => RuntimeDebugManager.CreateFromBootIfNeeded());
        if (HasFailed)
        {
            yield break;
        }

        // SoundManager
        yield return BootManager(
            nameof(SoundManager),
            () => SoundManager.CreateFromBootIfNeeded());
        if (HasFailed)
        {
            yield break;
        }

        // SceneLoader
        yield return BootManager(
            nameof(SceneLoader),
            () => SceneLoader.CreateFromBootIfNeeded());
        if (HasFailed)
        {
            yield break;
        }

        // GamePhaseManager
        yield return BootManager(
            nameof(GamePhaseManager),
            () => GamePhaseManager.CreateFromBootIfNeeded());
        if (HasFailed)
        {
            yield break;
        }

        // SceneProfileManager
        yield return BootManager(
            nameof(SceneProfileManager),
            () => SceneProfileManager.CreateFromBootIfNeeded());
        if (HasFailed)
        {
            yield break;
        }

        // PauseManager
        yield return BootManager(
            nameof(PauseManager),
            () => PauseManager.CreateFromBootIfNeeded());
        if (HasFailed)
        {
            yield break;
        }

        /*
        yield return BootSingleton(
            nameof(SceneLoader),
            () => SceneLoader.CreateFromBootIfNeeded());
        if (HasFailed) yield break;

        // 例2: 初期化が必要なManager
        yield return BootManager(
            nameof(RootUIManager),
            () => RootUIManager.CreateFromBootIfNeeded());
        if (HasFailed) yield break;

        yield return BootManager(
            nameof(OptionManager),
            () => OptionManager.CreateFromBootIfNeeded());
        if (HasFailed) yield break;
        */

        // yield return BootManager(
        //     nameof(SaveManager),
        //     () => SaveManager.CreateFromBootIfNeeded());
        // if (HasFailed) yield break;
        //
        // yield return BootManager(
        //     nameof(AudioManager),
        //     () => AudioManager.CreateFromBootIfNeeded());
        // if (HasFailed) yield break;
        // ------------------------------------------------------------

        IsCompleted = true;
        IsRunning = false;

        Debug.Log("[AppBootstrapper] Boot completed.");
    }

    /// <summary>
    /// 初期化不要の常駐Singleton生成。
    /// </summary>
    private IEnumerator BootSingleton<T>(string stepName, Func<T> createFunc)
        where T : MonoBehaviour
    {
        T instance;

        try
        {
            instance = createFunc.Invoke();
        }
        catch (Exception ex)
        {
            Fail($"{stepName} creation threw an exception.\n{ex}");
            yield break;
        }

        if (instance == null)
        {
            Fail($"{stepName} creation failed.");
            yield break;
        }
    }

    /// <summary>
    /// AppManagerBase 派生Managerの生成 + 初期化完了待ち。
    /// </summary>
    private IEnumerator BootManager<T>(string stepName, Func<T> createFunc)
        where T : AppManagerBase<T>
    {
        T instance;

        try
        {
            instance = createFunc.Invoke();
        }
        catch (Exception ex)
        {
            Fail($"{stepName} creation threw an exception.\n{ex}");
            yield break;
        }

        if (instance == null)
        {
            Fail($"{stepName} creation failed.");
            yield break;
        }

        // すでに初期化済みなら待たない
        if (instance.IsInitialized)
            yield break;

        // 初期化開始
        StartCoroutine(instance.InitializeIfNeeded());

        float timer = 0f;

        while (!instance.IsInitialized)
        {
            if (instance.IsFailed)
            {
                Fail($"{stepName} initialization failed.");
                yield break;
            }

            timer += Time.unscaledDeltaTime;
            if (timer >= initializeTimeoutSeconds)
            {
                Fail($"{stepName} initialization timed out. ({initializeTimeoutSeconds:0.###} sec)");
                yield break;
            }

            yield return null;
        }
    }

    private void Fail(string message)
    {
        HasFailed = true;
        IsRunning = false;
        Debug.LogError($"[AppBootstrapper] {message}");
    }
}
