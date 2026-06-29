using System.Collections;
using System.Collections.Generic;
using Pose;
using UnityEngine;

public class ForwardProgressMover : MonoBehaviour
{
    // Stage終了判定用のオブジェクト
    [SerializeField] private Transform _stageEndJudgeTransform;
    [SerializeField] private Transform _cameraTransform;

    // Pose状態を取得するContext
    [SerializeField] private string _poseContextName = "PoseContext";
    [SerializeField] private PoseContext _poseContext;

    // 自動登録の待ち時間
    [SerializeField, Min(0f)] private float _autoRegisterTimeout = 5f;

    [SerializeField, Min(0f)] private float _moveSpeed = 1f;

    // 前進させる対象
    private readonly List<Transform> _moveTargets = new();
    private StageEndJudge _stageEndJudge;
    private PoseStateTracker _poseStateTracker;
    private bool _poseStateTrackerWarningLogged;
    private bool _canMove;
    private bool _isStopped;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //　判定用オブジェクト
        AddMoveTarget(_stageEndJudgeTransform);

        //　カメラ
        AddMoveTarget(_cameraTransform);

        //　ステージ終了判定
        SetupStageEndJudge();

        //　Pose状態
        SetupPoseStateTracker(true);

        //　プレイヤーと敵
        StartCoroutine(RegisterPlayerAndEnemiesCoroutine());
    }

    private void OnDestroy()
    {
        if (_stageEndJudge == null)
        {
            return;
        }

        _stageEndJudge.OnStageEnd -= StopMove;
    }

    // Update is called once per frame
    void Update()
    {
        if (!_canMove || _isStopped)
        {
            return;
        }

        if (IsAnyPosing())
        {
            return;
        }

        // 前進させる
        float moveDistance = _moveSpeed * Time.deltaTime;
        foreach (var target in _moveTargets)
        {
            if (target == null)
            {
                continue;
            }

            target.position += Vector3.forward * moveDistance;
        }
    }

    private IEnumerator RegisterPlayerAndEnemiesCoroutine()
    {
        // Playerと敵が生成されるまで待って追加する
        float elapsedTime = 0f;
        bool playerRegistered = false;
        bool enemiesRegistered = false;

        while (elapsedTime <= _autoRegisterTimeout && (!playerRegistered || !enemiesRegistered))
        {
            //　プレイヤー
            if (!playerRegistered)
            {
                playerRegistered = TryRegisterPlayer();
            }

            //　敵
            if (!enemiesRegistered)
            {
                enemiesRegistered = TryRegisterEnemies();
            }

            if (playerRegistered && enemiesRegistered)
            {
                StartMove();
                yield break;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        if (!playerRegistered)
        {
            Debug.LogWarning("[ForwardProgressMover] Player target was not found.");
        }

        if (!enemiesRegistered)
        {
            Debug.LogWarning("[ForwardProgressMover] Enemy targets were not found.");
        }

        StartMove();
    }

    private bool TryRegisterPlayer()
    {
        // Player本体を探して追加
        Player.Proto_PlayerColorChanger player = Object.FindFirstObjectByType<Player.Proto_PlayerColorChanger>();
        if (player == null)
        {
            return false;
        }

        AddMoveTarget(player.transform);
        return true;
    }

    private bool TryRegisterEnemies()
    {
        // 登録済みの敵を探して追加
        Enemy.EnemyRegistry enemyRegistry = Object.FindFirstObjectByType<Enemy.EnemyRegistry>();
        if (enemyRegistry == null || enemyRegistry.Count <= 0)
        {
            return false;
        }

        foreach (Enemy.Enemy enemy in enemyRegistry.Enemies)
        {
            if (enemy == null)
            {
                continue;
            }

            AddMoveTarget(enemy.transform);
        }

        return true;
    }

    private void AddMoveTarget(Transform target)
    {
        // nullと重複は追加しない
        if (target == null || _moveTargets.Contains(target))
        {
            return;
        }

        _moveTargets.Add(target);
    }

    private void SetupStageEndJudge()
    {
        // Stage終了判定を探して停止処理を登録
        if (_stageEndJudgeTransform == null)
        {
            Debug.LogWarning("[ForwardProgressMover] StageEndJudge target was not set.");
            return;
        }

        _stageEndJudge = _stageEndJudgeTransform.GetComponent<StageEndJudge>();
        if (_stageEndJudge == null)
        {
            Debug.LogWarning("[ForwardProgressMover] StageEndJudge component was not found.");
            return;
        }

        _stageEndJudge.OnStageEnd += StopMove;
    }

    private void SetupPoseStateTracker(bool logWarning)
    {
        if (_poseStateTracker != null)
        {
            return;
        }

        if (_poseContext == null)
        {
            if (logWarning)
            {
                ContextManager contextManager = ContextManager.Instance;
                if (contextManager != null && !string.IsNullOrWhiteSpace(_poseContextName))
                {
                    _poseContext = contextManager.Get<PoseContext>(_poseContextName);
                }
            }
            else
            {
                _poseContext = Object.FindFirstObjectByType<PoseContext>();
            }
        }

        _poseStateTracker = _poseContext?.GetPoseStateTracker;
        if (_poseStateTracker != null || !logWarning || _poseStateTrackerWarningLogged)
        {
            return;
        }

        _poseStateTrackerWarningLogged = true;
        Debug.LogWarning("[ForwardProgressMover] PoseStateTracker was not found.");
    }

    private bool IsAnyPosing()
    {
        SetupPoseStateTracker(false);
        return _poseStateTracker != null && _poseStateTracker.IsAnyPosing;
    }

    private void StartMove()
    {
        if (_canMove || _isStopped)
        {
            return;
        }

        _canMove = true;
    }

    public void StopMove()
    {
        if (_isStopped)
        {
            return;
        }

        _isStopped = true;
        _canMove = false;
    }
}
