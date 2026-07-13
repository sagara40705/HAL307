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
    private Enemy.EnemyLineupManager enemyLineupManager;
    private Enemy.EnemyRegistry enemyRegistry;

    //現在見ているターゲット
    private Transform currentTarget;

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

        if (enemy1 != null)
        {
            enemyL = enemy1.transform;
        }

        if (enemy2 != null)
        {
            enemyR = enemy2.transform;
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
                enemyL = enemy.transform;
            }
            else if (enemy.name == Enemy.EnemySlotId.Enemy2.ToString())
            {
                enemyR = enemy.transform;
            }
        }
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
