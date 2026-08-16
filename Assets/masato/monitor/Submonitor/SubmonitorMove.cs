using UnityEngine;
using UnityEngine.InputSystem;

public class SubmonitorMove : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    //何秒かけて移動するか
    [SerializeField, Min(0.01f)] private float moveDuration = 0.5f;
    [SerializeField, Min(0.01f)] private float cameraDistance = 5f;
    [SerializeField] private float cameraHeight = 1.8f;
    [SerializeField] private float lookAtHeight = 1.2f;
    private float moveTimer = 0f;

    //それぞれのTransformを格納する変数
    private Player.PlayerPoseContoroller playerPoseController;
    private Transform player;
    private Transform enemyL;
    private Transform enemyR;
    private Transform enemy3;
    private Transform enemy4;
    private Enemy.Enemy enemyLController;
    private Enemy.Enemy enemyRController;
    private Enemy.Enemy enemy3Controller;
    private Enemy.Enemy enemy4Controller;
    private Enemy.EnemyLineupManager enemyLineupManager;
    private Enemy.EnemyRegistry enemyRegistry;

    //現在見ているターゲット
    private Transform currentTarget;
    private Transform poseFocusTarget;

    //ポーズ開始を検出するための前フレーム状態
    private bool wasPlayerPosing;
    private bool wasEnemyLPosing;
    private bool wasEnemyRPosing;
    private bool wasEnemy3Posing;
    private bool wasEnemy4Posing;
    private bool isPoseFocusActive;

    //移動の始点を格納する変数
    private Vector3 startPosition;
    private Quaternion startRotation;
    private float orbitAngle;

    //カメラの前方停止フラグ
    private bool isStopFront = false;
    private bool restartRotationAfterMove = false;

    //移動しているかのフラグ
    private bool isMoving = false;
    private bool isCameraInitialized = false;

    void Start()
    {
        ResolvePlayer();
        ResolveEnemies();
        SetupCamera();
    }

    void Update()
    {
        ResolvePlayer();
        ResolveEnemies();
        HandleInput();
    }

    private void LateUpdate()
    {
        ResolvePlayer();
        ResolveEnemies();
        UpdatePoseFocus();

        if (!isCameraInitialized)
        {
            SetupCamera();
        }

        if (!isCameraInitialized || player == null)
        {
            return;
        }

        if (isMoving)
        {
            UpdateMove();
            return;
        }

        if (isStopFront)
        {
            if (currentTarget == null)
            {
                ResumeOrbit();
                return;
            }

            FollowTargetFront(currentTarget);
            return;
        }

        UpdateOrbit();
    }

    private void HandleInput()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            if (isStopFront)
            {
                if (currentTarget == player && !isMoving)
                {
                    ResumeOrbit();
                }
                else
                {
                    MoveToFront(player, true);
                }
            }
            else
            {
                MoveToFront(player, false);
            }
        }

        if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
        {
            if (currentTarget == enemyL)
            {
                MoveToFront(player, true);
            }
            else
            {
                MoveToFront(enemyL, false);
            }
        }

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
        {
            if (currentTarget == enemyR)
            {
                MoveToFront(player, true);
            }
            else
            {
                MoveToFront(enemyR,false);
            }
        }

    }

    private void MoveToFront(Transform target, bool restartRotation)
    {
        if (target == null)
        {
            return;
        }

        isStopFront = true;

        startPosition = transform.position;
        startRotation = transform.rotation;

        moveTimer = 0f;
        isMoving = true;

        currentTarget = target;
        restartRotationAfterMove = restartRotation;
    }

    private void ResolvePlayer()
    {
        if (player != null)
        {
            return;
        }

        if (playerPoseController == null)
        {
            playerPoseController = Object.FindFirstObjectByType<Player.PlayerPoseContoroller>();
        }

        player = playerPoseController != null
            ? playerPoseController.MoveTargetTransform
            : null;

        if (isStopFront && currentTarget == null)
        {
            currentTarget = player;
        }
    }

    private void ResolveEnemies()
    {
        ResolveEnemiesFromLineup();

        if (enemyL != null && enemyR != null)
        {
            return;
        }

        ResolveEnemiesFromRegistry();
    }

    private void ResolveEnemiesFromLineup()
    {
        if (enemyLineupManager == null)
        {
            enemyLineupManager = Object.FindFirstObjectByType<Enemy.EnemyLineupManager>();
        }

        if (enemyLineupManager == null)
        {
            return;
        }

        Enemy.Enemy enemy1 = enemyLineupManager.Enemy1;
        Enemy.Enemy enemy2 = enemyLineupManager.Enemy2;
        Enemy.Enemy lineupEnemy3 = enemyLineupManager.Enemy3;
        Enemy.Enemy lineupEnemy4 = enemyLineupManager.Enemy4;

        if (enemy1 != null)
        {
            enemyLController = enemy1;
            enemyL = enemy1.transform;
        }

        if (enemy2 != null)
        {
            enemyRController = enemy2;
            enemyR = enemy2.transform;
        }

        if (lineupEnemy3 != null)
        {
            enemy3Controller = lineupEnemy3;
            enemy3 = lineupEnemy3.transform;
        }

        if (lineupEnemy4 != null)
        {
            enemy4Controller = lineupEnemy4;
            enemy4 = lineupEnemy4.transform;
        }
    }

    private void ResolveEnemiesFromRegistry()
    {
        if (enemyRegistry == null)
        {
            enemyRegistry = Object.FindFirstObjectByType<Enemy.EnemyRegistry>();
        }

        if (enemyRegistry == null)
        {
            return;
        }

        foreach (Enemy.Enemy enemy in enemyRegistry.Enemies)
        {
            if (enemy == null)
            {
                continue;
            }

            if (enemy.name == Enemy.EnemySlotId.Enemy1.ToString())
            {
                enemyLController = enemy;
                enemyL = enemy.transform;
            }
            else if (enemy.name == Enemy.EnemySlotId.Enemy2.ToString())
            {
                enemyRController = enemy;
                enemyR = enemy.transform;
            }
            else if (enemy.name == Enemy.EnemySlotId.Enemy3.ToString())
            {
                enemy3Controller = enemy;
                enemy3 = enemy.transform;
            }
            else if (enemy.name == Enemy.EnemySlotId.Enemy4.ToString())
            {
                enemy4Controller = enemy;
                enemy4 = enemy.transform;
            }
        }
    }

    private void UpdatePoseFocus()
    {
        bool isPlayerPosing = playerPoseController != null && playerPoseController.IsPosing;
        bool isEnemyLPosing = enemyLController != null && enemyLController.IsPosing;
        bool isEnemyRPosing = enemyRController != null && enemyRController.IsPosing;
        bool isEnemy3Posing = enemy3Controller != null && enemy3Controller.IsPosing;
        bool isEnemy4Posing = enemy4Controller != null && enemy4Controller.IsPosing;

        // 同フレームではPlayerを最優先し、番号が大きいEnemyを次に優先する
        Transform startedPoseTarget = null;
        if (isEnemyLPosing && !wasEnemyLPosing)
        {
            startedPoseTarget = enemyL;
        }
        if (isEnemyRPosing && !wasEnemyRPosing)
        {
            startedPoseTarget = enemyR;
        }
        if (isEnemy3Posing && !wasEnemy3Posing)
        {
            startedPoseTarget = enemy3;
        }
        if (isEnemy4Posing && !wasEnemy4Posing)
        {
            startedPoseTarget = enemy4;
        }
        if (isPlayerPosing && !wasPlayerPosing)
        {
            startedPoseTarget = player;
        }

        if (startedPoseTarget != null)
        {
            FocusPosingActor(startedPoseTarget);
        }
        else if (!isPoseFocusActive)
        {
            Transform posingTarget = GetPosingTarget(isPlayerPosing, isEnemyLPosing, isEnemyRPosing, isEnemy3Posing, isEnemy4Posing);
            if (posingTarget != null)
            {
                FocusPosingActor(posingTarget);
            }
        }
        else if (!IsTargetPosing(poseFocusTarget, isPlayerPosing, isEnemyLPosing, isEnemyRPosing, isEnemy3Posing, isEnemy4Posing))
        {
            Transform posingTarget = GetPosingTarget(isPlayerPosing, isEnemyLPosing, isEnemyRPosing, isEnemy3Posing, isEnemy4Posing);
            if (posingTarget != null)
            {
                FocusPosingActor(posingTarget);
            }
            else
            {
                isPoseFocusActive = false;
                poseFocusTarget = null;
                ResumeOrbit();
            }
        }
        else if (currentTarget != poseFocusTarget || !isStopFront)
        {
            MoveToFront(poseFocusTarget, false);
        }

        wasPlayerPosing = isPlayerPosing;
        wasEnemyLPosing = isEnemyLPosing;
        wasEnemyRPosing = isEnemyRPosing;
        wasEnemy3Posing = isEnemy3Posing;
        wasEnemy4Posing = isEnemy4Posing;
    }

    private void FocusPosingActor(Transform target)
    {
        if (target == null)
        {
            return;
        }

        isPoseFocusActive = true;
        poseFocusTarget = target;
        MoveToFront(target, false);
    }

    private Transform GetPosingTarget(
        bool isPlayerPosing,
        bool isEnemyLPosing,
        bool isEnemyRPosing,
        bool isEnemy3Posing,
        bool isEnemy4Posing)
    {
        if (isPlayerPosing && player != null)
        {
            return player;
        }
        if (isEnemyLPosing && enemyL != null)
        {
            return enemyL;
        }
        if (isEnemyRPosing && enemyR != null)
        {
            return enemyR;
        }
        if (isEnemy3Posing && enemy3 != null)
        {
            return enemy3;
        }
        if (isEnemy4Posing && enemy4 != null)
        {
            return enemy4;
        }

        return null;
    }

    private bool IsTargetPosing(
        Transform target,
        bool isPlayerPosing,
        bool isEnemyLPosing,
        bool isEnemyRPosing,
        bool isEnemy3Posing,
        bool isEnemy4Posing)
    {
        if (target == null)
        {
            return false;
        }

        return (target == player && isPlayerPosing) ||
               (target == enemyL && isEnemyLPosing) ||
               (target == enemyR && isEnemyRPosing) ||
               (target == enemy3 && isEnemy3Posing) ||
               (target == enemy4 && isEnemy4Posing);
    }

    private void SetupCamera()
    {
        if (isCameraInitialized || player == null)
        {
            return;
        }

        transform.SetParent(null, true);
        orbitAngle = player.eulerAngles.y;
        SetCameraPose(GetFrontPosition(player), player);
        isCameraInitialized = true;
    }

    private void UpdateMove()
    {
        if (currentTarget == null)
        {
            isMoving = false;
            ResumeOrbit();
            return;
        }

        moveTimer += Time.deltaTime;
        float duration = Mathf.Max(0.01f, moveDuration);
        float t = Mathf.Clamp01(moveTimer / duration);
        float easedT = t * t * (3f - 2f * t);

        Vector3 destination = GetFrontPosition(currentTarget);
        Quaternion destinationRotation = GetLookRotation(currentTarget, destination);
        transform.position = Vector3.Lerp(startPosition, destination, easedT);
        transform.rotation = Quaternion.Slerp(startRotation, destinationRotation, easedT);

        if (t < 1f)
        {
            return;
        }

        isMoving = false;
        if (restartRotationAfterMove)
        {
            ResumeOrbit();
        }
    }

    private void FollowTargetFront(Transform target)
    {
        SetCameraPose(GetFrontPosition(target), target);
    }

    private void UpdateOrbit()
    {
        orbitAngle += moveSpeed * Time.deltaTime;
        Vector3 orbitOffset = Quaternion.Euler(0f, orbitAngle, 0f) * Vector3.forward * cameraDistance;
        Vector3 cameraPosition = player.position + orbitOffset + Vector3.up * cameraHeight;
        SetCameraPose(cameraPosition, player);
    }

    private void ResumeOrbit()
    {
        isMoving = false;
        isStopFront = false;
        restartRotationAfterMove = false;
        currentTarget = player;

        if (player == null)
        {
            return;
        }

        Vector3 offset = Vector3.ProjectOnPlane(transform.position - player.position, Vector3.up);
        if (offset.sqrMagnitude > 0.0001f)
        {
            orbitAngle = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;
        }
    }

    private Vector3 GetFrontPosition(Transform target)
    {
        Vector3 forward = Vector3.ProjectOnPlane(target.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.0001f)
        {
            forward = Vector3.forward;
        }

        return target.position + forward * cameraDistance + Vector3.up * cameraHeight;
    }

    private Quaternion GetLookRotation(Transform target, Vector3 cameraPosition)
    {
        Vector3 lookDirection = target.position + Vector3.up * lookAtHeight - cameraPosition;
        return Quaternion.LookRotation(lookDirection, Vector3.up);
    }

    private void SetCameraPose(Vector3 cameraPosition, Transform target)
    {
        transform.position = cameraPosition;
        transform.rotation = GetLookRotation(target, cameraPosition);
    }
}
