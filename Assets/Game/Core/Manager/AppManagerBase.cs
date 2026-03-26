using System.Collections;
using UnityEngine;

public enum AppManagerInitializeState
{
    None,
    Initializing,
    Initialized,
    Failed,
}

/// <summary>
/// App 常駐 Manager 基底。
/// Boot で生成したあと、Boot 側から InitializeIfNeeded を順番に呼ぶ。
/// </summary>
public abstract class AppManagerBase<T> : AppLifetimeSingleton<T>
    where T : AppManagerBase<T>
{
    public AppManagerInitializeState InitializeState { get; private set; } = AppManagerInitializeState.None;

    public bool IsInitialized => InitializeState == AppManagerInitializeState.Initialized;
    public bool IsInitializing => InitializeState == AppManagerInitializeState.Initializing;
    public bool IsFailed => InitializeState == AppManagerInitializeState.Failed;

    /// <summary>
    /// Boot から呼ぶ初期化入口。
    /// 二重呼び出しされても安全。
    /// </summary>
    public IEnumerator InitializeIfNeeded()
    {
        if (InitializeState == AppManagerInitializeState.Initialized)
            yield break;

        if (InitializeState == AppManagerInitializeState.Initializing)
        {
            while (InitializeState == AppManagerInitializeState.Initializing)
                yield return null;

            yield break;
        }

        if (InitializeState == AppManagerInitializeState.Failed)
        {
            Debug.LogError($"[{typeof(T).Name}] initialize was already failed.");
            yield break;
        }

        InitializeState = AppManagerInitializeState.Initializing;

        yield return OnInitialize();

        if (InitializeState != AppManagerInitializeState.Failed)
        {
            InitializeState = AppManagerInitializeState.Initialized;
        }
    }

    /// <summary>
    /// 各 Manager の実初期化処理。
    /// 非同期処理があるならここで yield return する。
    /// </summary>
    protected abstract IEnumerator OnInitialize();

    /// <summary>
    /// 初期化失敗時に呼ぶ。
    /// </summary>
    protected void FailInitialize(string message)
    {
        InitializeState = AppManagerInitializeState.Failed;
        Debug.LogError($"[{typeof(T).Name}] initialize failed. {message}");
    }
}
