using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ForwardProgressMover : MonoBehaviour
{
    [SerializeField] private Transform _stageEndJudgeTransform;
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private ForwardProgressMoveSettings _moveSettings;

    [SerializeField, Min(0f)] private float _autoRegisterTimeout = 5f;
    [SerializeField, Min(0f)] private float _fallbackEnemyMoveSpeed = 1f;
    [SerializeField, Min(0f)] private float _fallbackPlayerMoveSpeed = 1f;
    [SerializeField, Range(0f, 1f)] private float _fallbackLeaderSpeedMultiplier = 0.5f;

    private readonly List<MoveActor> _moveActors = new();
    private StageEndJudge _stageEndJudge;
    private bool _canMove;
    private bool _isStopped;
    private bool _hasLeaderFollowPosition;
    private float _lastLeaderForwardPosition;

    public Transform CameraTransform => _cameraTransform;

    private float EnemyMoveSpeed => _moveSettings != null ? _moveSettings.EnemyMoveSpeed : _fallbackEnemyMoveSpeed;
    private float PlayerMoveSpeed => _moveSettings != null ? _moveSettings.PlayerMoveSpeed : _fallbackPlayerMoveSpeed;
    private float LeaderSpeedMultiplier => _moveSettings != null ? _moveSettings.LeaderSpeedMultiplier : _fallbackLeaderSpeedMultiplier;

    private void Start()
    {
        SetupStageEndJudge();
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

    private void Update()
    {
        if (!_canMove || _isStopped)
        {
            return;
        }

        if (InGameFlowLock.IsPresentationPlaying)
        {
            ResetLeaderFollowPosition();
            return;
        }

        MoveActors(Time.deltaTime);
        MoveFollowersToLeader();
    }

    private IEnumerator RegisterPlayerAndEnemiesCoroutine()
    {
        float elapsedTime = 0f;
        bool playerRegistered = false;
        bool enemiesRegistered = false;

        while (elapsedTime <= _autoRegisterTimeout && (!playerRegistered || !enemiesRegistered))
        {
            if (!playerRegistered)
            {
                playerRegistered = TryRegisterPlayer();
            }

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
        Player.PlayerPoseContoroller player = FindFirstObjectByType<Player.PlayerPoseContoroller>();
        if (player == null)
        {
            return false;
        }

        Transform playerMoveTarget = player.MoveTargetTransform;
        if (playerMoveTarget == null)
        {
            return false;
        }

        AddOrUpdateActor(playerMoveTarget, MoveActorKind.Player, null, player);
        return true;
    }

    private bool TryRegisterEnemies()
    {
        Enemy.EnemyRegistry enemyRegistry = FindFirstObjectByType<Enemy.EnemyRegistry>();
        if (enemyRegistry == null || enemyRegistry.Count < 2)
        {
            return false;
        }

        int registeredCount = 0;
        foreach (Enemy.Enemy enemy in enemyRegistry.Enemies)
        {
            if (enemy == null)
            {
                continue;
            }

            AddOrUpdateActor(enemy.transform, MoveActorKind.Enemy, enemy, null);
            registeredCount++;
        }

        return registeredCount >= 2;
    }

    private void MoveActors(float deltaTime)
    {
        RemoveMissingActors();

        if (_moveActors.Count == 0)
        {
            ResetLeaderFollowPosition();
            return;
        }

        float leaderForwardPosition = GetLeaderForwardPosition();
        float rearForwardPosition = GetRearForwardPosition();
        bool shouldSlowLeader = leaderForwardPosition - rearForwardPosition > 0.01f;
        float leaderMultiplier = Mathf.Clamp01(LeaderSpeedMultiplier);

        foreach (MoveActor actor in _moveActors)
        {
            if (actor == null || actor.Transform == null || actor.IsPosing)
            {
                continue;
            }

            float speed = actor.Kind == MoveActorKind.Player ? PlayerMoveSpeed : EnemyMoveSpeed;
            if (shouldSlowLeader && IsLeader(actor, leaderForwardPosition))
            {
                speed *= leaderMultiplier;
            }

            actor.Transform.position += Vector3.forward * speed * deltaTime;
        }
    }

    private void MoveFollowersToLeader()
    {
        RemoveMissingActors();

        if (_moveActors.Count == 0)
        {
            ResetLeaderFollowPosition();
            return;
        }

        float leaderForwardPosition = GetLeaderForwardPosition();
        if (!_hasLeaderFollowPosition)
        {
            _lastLeaderForwardPosition = leaderForwardPosition;
            _hasLeaderFollowPosition = true;
            return;
        }

        float moveDistance = leaderForwardPosition - _lastLeaderForwardPosition;
        _lastLeaderForwardPosition = leaderForwardPosition;

        if (moveDistance <= 0f)
        {
            return;
        }

        MoveFollower(_cameraTransform, moveDistance);
        MoveFollower(_stageEndJudgeTransform, moveDistance);
    }

    private void MoveFollower(Transform target, float moveDistance)
    {
        if (target == null)
        {
            return;
        }

        target.position += Vector3.forward * moveDistance;
    }

    private float GetLeaderForwardPosition()
    {
        float leaderForwardPosition = float.NegativeInfinity;
        foreach (MoveActor actor in _moveActors)
        {
            if (actor == null || actor.Transform == null)
            {
                continue;
            }

            leaderForwardPosition = Mathf.Max(leaderForwardPosition, actor.Transform.position.z);
        }

        return leaderForwardPosition;
    }

    private float GetRearForwardPosition()
    {
        float rearForwardPosition = float.PositiveInfinity;
        foreach (MoveActor actor in _moveActors)
        {
            if (actor == null || actor.Transform == null)
            {
                continue;
            }

            rearForwardPosition = Mathf.Min(rearForwardPosition, actor.Transform.position.z);
        }

        return rearForwardPosition;
    }

    private bool IsLeader(MoveActor actor, float leaderForwardPosition)
    {
        if (actor == null || actor.Transform == null)
        {
            return false;
        }

        return Mathf.Abs(actor.Transform.position.z - leaderForwardPosition) <= 0.01f;
    }

    private void AddOrUpdateActor(Transform target, MoveActorKind kind, Enemy.Enemy enemy, Player.PlayerPoseContoroller player)
    {
        if (target == null)
        {
            return;
        }

        MoveActor actor = FindActor(target);
        if (actor == null)
        {
            _moveActors.Add(new MoveActor(target, kind, enemy, player));
            ResetLeaderFollowPosition();
            return;
        }

        actor.Update(kind, enemy, player);
    }

    private MoveActor FindActor(Transform target)
    {
        foreach (MoveActor actor in _moveActors)
        {
            if (actor != null && actor.Transform == target)
            {
                return actor;
            }
        }

        return null;
    }

    private void RemoveMissingActors()
    {
        for (int i = _moveActors.Count - 1; i >= 0; i--)
        {
            if (_moveActors[i] == null || _moveActors[i].Transform == null)
            {
                _moveActors.RemoveAt(i);
                ResetLeaderFollowPosition();
            }
        }
    }

    private void SetupStageEndJudge()
    {
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

    private void StartMove()
    {
        if (_canMove || _isStopped)
        {
            return;
        }

        ResetLeaderFollowPosition();
        _canMove = true;
    }

    private void ResetLeaderFollowPosition()
    {
        _hasLeaderFollowPosition = false;
        _lastLeaderForwardPosition = 0f;
    }

    public void StopMove()
    {
        if (_isStopped)
        {
            return;
        }

        _isStopped = true;
        _canMove = false;
        ResetLeaderFollowPosition();
    }

    public void ResumeMove()
    {
        _isStopped = false;
        _canMove = true;
        ResetLeaderFollowPosition();
    }

    public void RegisterMoveTarget(Transform target)
    {
        if (target == null)
        {
            return;
        }

        Enemy.Enemy enemy = target.GetComponentInParent<Enemy.Enemy>();
        if (enemy != null)
        {
            AddOrUpdateActor(enemy.transform, MoveActorKind.Enemy, enemy, null);
            return;
        }

        Player.PlayerPoseContoroller player = target.GetComponentInParent<Player.PlayerPoseContoroller>();
        if (player != null)
        {
            AddOrUpdateActor(player.MoveTargetTransform, MoveActorKind.Player, null, player);
            return;
        }

        AddOrUpdateActor(target, MoveActorKind.Enemy, null, null);
    }

    public void UnregisterMoveTarget(Transform target)
    {
        if (target == null)
        {
            return;
        }

        for (int i = _moveActors.Count - 1; i >= 0; i--)
        {
            MoveActor actor = _moveActors[i];
            if (actor == null || actor.Transform == target || actor.Transform == null || actor.Transform.IsChildOf(target))
            {
                _moveActors.RemoveAt(i);
            }
        }

        ResetLeaderFollowPosition();
    }

    private enum MoveActorKind
    {
        Enemy,
        Player
    }

    private sealed class MoveActor
    {
        public MoveActor(Transform transform, MoveActorKind kind, Enemy.Enemy enemy, Player.PlayerPoseContoroller player)
        {
            Transform = transform;
            Update(kind, enemy, player);
        }

        public Transform Transform { get; }
        public MoveActorKind Kind { get; private set; }
        public Enemy.Enemy Enemy { get; private set; }
        public Player.PlayerPoseContoroller Player { get; private set; }
        public bool IsPosing => Enemy != null ? Enemy.IsPosing : Player != null && Player.IsPosing;

        public void Update(MoveActorKind kind, Enemy.Enemy enemy, Player.PlayerPoseContoroller player)
        {
            Kind = kind;
            Enemy = enemy;
            Player = player;
        }
    }
}
