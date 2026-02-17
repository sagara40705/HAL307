using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// RootUIRefs（RootUIシーンに置く参照束）
///
/// 目的：
/// - RootUIシーン上に配置した共通UI部品（LoadingView等）を、
///   RootUIManagerがまとめて参照できるようにする。
///
/// ルール：
/// - RootUIシーン内に RootUIRefs は「1つだけ」置く（Findで拾う前提のため）
/// - ここにドラッグ&ドロップで参照を割り当てる
/// </summary>
public sealed class RootUIRefs : MonoBehaviour
{
    [Header("Persist Target (recommended)")]
    [Tooltip("DontDestroyOnLoad に移すルート。未設定なら RootUIRefs の最上位ルートを使う。")]
    [SerializeField] private GameObject persistRoot;

    [Header("Common Views")]
    [SerializeField] public LoadingView LoadingView;
    [SerializeField] public FadeView FadeView;

    public GameObject PersistRoot => persistRoot;
}
