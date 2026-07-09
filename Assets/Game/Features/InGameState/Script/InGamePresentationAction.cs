using System.Collections;
using UnityEngine;

// インゲーム進行Stateから実行する演出データ
public abstract class InGamePresentationAction : ScriptableObject
{
    // 演出処理の実行
    public abstract IEnumerator Play(InGameFlowContext context);
}
