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

        // 起動順は依存関係に合わせて維持する。各ステップは実行時に生成・初期化する。
        IEnumerator[] steps =
        {
            BootManager(nameof(ContextManager), ContextManager.CreateFromBootIfNeeded),
            BootManager(nameof(RuntimeDebugManager), RuntimeDebugManager.CreateFromBootIfNeeded),
            BootManager(nameof(SoundManager), SoundManager.CreateFromBootIfNeeded),
            BootManager(nameof(SceneLoader), SceneLoader.CreateFromBootIfNeeded),
            BootManager(nameof(GamePhaseManager), GamePhaseManager.CreateFromBootIfNeeded),
            BootManager(nameof(GameModeManager), GameModeManager.CreateFromBootIfNeeded),
            BootManager(nameof(GameResultManager), GameResultManager.CreateFromBootIfNeeded),
            BootManager(nameof(SceneProfileManager), SceneProfileManager.CreateFromBootIfNeeded),
            BootManager(nameof(PauseManager), PauseManager.CreateFromBootIfNeeded),
        };

        foreach (IEnumerator step in steps)
        {
            yield return step;
            if (HasFailed)
            {
                yield break;
            }
        }

        IsCompleted = true;
        IsRunning = false;

        Debug.Log("[AppBootstrapper] Boot completed.");
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
        StartCoroutine(CoroutineUtility.Run(instance.InitializeIfNeeded()));

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
