using System;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class StageEndJudge : MonoBehaviour
{
    // 当たったか判定する対象
    [SerializeField] private GameObject _targetObject;

    // 一度だけ判定するか
    [SerializeField] private bool _isOnce = true;

    // 当たった時に実行する処理
    [SerializeField] private UnityEvent _onStageEnd;

    public event Action OnStageEnd;

    private bool _isJudged;

    private bool _canLoadNextScene;

    private void Awake()
    {
        SetTriggerColliders();
    }

    private void OnValidate()
    {
        SetTriggerColliders();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isOnce && _isJudged) { return; }
        if (!IsTargetObject(other)) { return; }

        _isJudged = true;
        Debug.Log("StageEndJudge: Stage End Triggered by " + other.name);
        OnStageEnd?.Invoke();
        _onStageEnd?.Invoke();
    }

    private bool IsTargetObject(Collider other)
    {
        if (_targetObject == null) { return false; }
        if (other == null) { return false; }

        Transform targetTransform = _targetObject.transform;
        return other.gameObject == _targetObject ||
               other.transform == targetTransform ||
               other.transform.IsChildOf(targetTransform);
    }

    private void SetTriggerColliders()
    {
        // 自分のColliderをTriggerにする
        SetTrigger(GetComponent<Collider>());

        if (_targetObject == null) { return; }

        // 対象のColliderもTriggerにする
        Collider[] targetColliders = _targetObject.GetComponentsInChildren<Collider>();
        foreach (Collider targetCollider in targetColliders)
        {
            SetTrigger(targetCollider);
        }
    }

    private static void SetTrigger(Collider targetCollider)
    {
        if (targetCollider == null) { return; }

        targetCollider.isTrigger = true;
    }

    public void CanLoadScene()
    {

    }
}
