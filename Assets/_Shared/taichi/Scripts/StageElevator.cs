using System.Collections;
using UnityEngine;

public class StageElevator : MonoBehaviour
{
    [SerializeField] private Transform _elevatorLeftTf;
    [SerializeField] private Transform _elevatorCenterTf;
    [SerializeField] private Transform _elevatorRightTf;

    [SerializeField, Min(0f)] private float _moveDuration = 3.0f;
    [SerializeField, Min(0f)] private float _moveSpeed = 1.0f;

    private float _moveDistance = 0f;
    private Coroutine _moveCoroutine;

    private void Awake()
    {
        _moveDistance = _moveSpeed * _moveDuration;
    }

    public void StartElevator()
    {
        _moveDistance = _moveSpeed * _moveDuration;

        if (_moveCoroutine != null)
        {
            StopCoroutine(_moveCoroutine);
        }

        _moveCoroutine = StartCoroutine(MoveElevators());
    }

    private IEnumerator MoveElevators()
    {
        Transform[] targets = { _elevatorLeftTf, _elevatorCenterTf, _elevatorRightTf };
        Vector3[] targetPositions = new Vector3[targets.Length];

        bool hasTarget = false;
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null)
            {
                continue;
            }

            hasTarget = true;
            targetPositions[i] = targets[i].position;
            targets[i].position = targetPositions[i] + Vector3.down * _moveDistance;
        }

        if (!hasTarget)
        {
            _moveCoroutine = null;
            yield break;
        }

        if (_moveDuration <= 0f || _moveSpeed <= 0f)
        {
            ApplyPositions(targets, targetPositions);
            _moveCoroutine = null;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < _moveDuration)
        {
            float moveTime = Mathf.Min(Time.deltaTime, _moveDuration - elapsed);

            for (int i = 0; i < targets.Length; i++)
            {
                if (targets[i] == null)
                {
                    continue;
                }

                targets[i].position += Vector3.up * _moveSpeed * moveTime;
            }

            elapsed += moveTime;
            yield return null;
        }

        ApplyPositions(targets, targetPositions);

        _moveCoroutine = null;
    }

    private void ApplyPositions(Transform[] targets, Vector3[] positions)
    {
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i] == null)
            {
                continue;
            }

            targets[i].position = positions[i];
        }
    }
}
