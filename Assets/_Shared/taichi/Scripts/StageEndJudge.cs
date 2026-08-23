using System;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider))]
public class StageEndJudge : MonoBehaviour
{
    private const string DebugWindowName = "Stage";

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

    private void Start()
    {
        CreateDebugMenu();
    }

    private void OnDestroy()
    {
        RuntimeDebug.DebugMenu.DestroyWindow(DebugWindowName);
    }

    private void OnValidate()
    {
        SetTriggerColliders();
    }

    private void OnTriggerEnter(Collider other)
    {
        TryNotifyStageEnd(other);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!_isOnce) { return; }

        TryNotifyStageEnd(other);
    }

    private void TryNotifyStageEnd(Collider other)
    {
        if (_isOnce && _isJudged) { return; }
        if (!IsTargetObject(other)) { return; }

        NotifyStageEnd(other.name);
    }

    public void ForceStageEnd()
    {
        ForceStageEndWithoutScore();
    }

    public void ForceStageEndWithoutScore()
    {
        if (_isOnce && _isJudged) { return; }

        NotifyStageEnd("Debug");
    }

    public void ForceStageEndWithTargetScore()
    {
        if (_isOnce && _isJudged) { return; }

        GiveCurrentWaveTargetScore();
        NotifyStageEnd("Debug Clear");
    }

    private void NotifyStageEnd(string sourceName)
    {
        _isJudged = true;
        Debug.Log("StageEndJudge: Stage End Triggered by " + sourceName);
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

    public void ResetJudge()
    {
        _isJudged = false;
    }

    private void GiveCurrentWaveTargetScore()
    {
        InGameFlowController flowController = FindFirstObjectByType<InGameFlowController>();
        if (flowController == null || flowController.CurrentWaveData == null)
        {
            Debug.LogWarning("[StageEndJudge] Target score was not added. Reason=Current wave is unavailable.");
            return;
        }

        ScoreManager scoreManager = ScoreManager.Instance != null ? ScoreManager.Instance : FindFirstObjectByType<ScoreManager>();
        if (scoreManager == null)
        {
            Debug.LogWarning("[StageEndJudge] Target score was not added. Reason=ScoreManager is null.");
            return;
        }

        int targetScore = flowController.CurrentTargetScore;
        scoreManager.SetPlayerScoreAtLeast(targetScore);
        Debug.Log($"[StageEndJudge] Player score forced to target. TargetScore={targetScore}");
    }

    private void CreateDebugMenu()
    {
        RuntimeDebug.DebugWindow window = RuntimeDebug.DebugMenu.CreateWindow(DebugWindowName);
        window.CreateButton("Force Wave Clear", ForceStageEndWithTargetScore);
        window.CreateButton("Force Wave Failed", ForceStageEndWithoutScore);
    }
}
