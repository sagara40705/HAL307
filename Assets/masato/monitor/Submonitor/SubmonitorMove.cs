using UnityEngine;
using UnityEngine.InputSystem;

public class SubmonitorMove : MonoBehaviour
{

    [SerializeField] private float moveSpeed = 5f;

    //何秒かけて移動するか
    [SerializeField] private float moveDuration = 0.5f;
    private float moveTimer = 0f;

    //それぞれのTransformを格納する変数
    private Transform player;
    private Transform enemyL;
    private Transform enemyR;
    private Enemy.EnemyLineupManager enemyLineupManager;
    private Enemy.EnemyRegistry enemyRegistry;

    //現在見ているターゲット
    private Transform currentTarget;

    //カメラのPivotを格納する変数
    private Transform cameraPivot;

    //移動の始点と終点を格納する変数
    private Vector3 startPosition;
    private Vector3 targetPosition;
    private Quaternion startRotation;
    private Quaternion targetRotation;

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
        SetupCameraPivot();
    }

    void Update()
    {
        if (!isCameraInitialized)
        {
            ResolvePlayer();
            SetupCameraPivot();
        }

        ResolveEnemies();

        if (isMoving)
        {
            moveTimer += Time.deltaTime;
            float t = moveTimer / moveDuration;
            t = Mathf.Clamp01(t);
            float easedT = t * t * (3f - 2f * t);
            transform.position = Vector3.Lerp(startPosition, targetPosition, easedT);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, easedT);

            if (t >= 1f)
            {
                isMoving = false;

                if (restartRotationAfterMove)
                {
                    isStopFront = false;
                }
            }

        }

        if (isStopFront == false && isMoving == false)
        {
            if (cameraPivot == null)
            {
                return;
            }

            cameraPivot.Rotate(0f, moveSpeed * Time.deltaTime, 0f);
        }

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.enterKey.wasPressedThisFrame)
        {
            if (isStopFront)
            {
                isMoving = false;
                isStopFront = false;
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
        targetPosition = target.position + target.forward * 5f + Vector3.up * 1.8f;

        startRotation = transform.rotation;
        targetRotation = Quaternion.LookRotation(target.position + Vector3.up * 1.2f - targetPosition);

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

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject == null)
        {
            return;
        }

        player = playerObject.transform;
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

    private void SetupCameraPivot()
    {
        if (isCameraInitialized || player == null)
        {
            return;
        }

        if (cameraPivot == null)
        {
            GameObject pivotObject = new GameObject("CameraPivot");
            cameraPivot = pivotObject.transform;
        }

        cameraPivot.SetParent(player);
        cameraPivot.localPosition = Vector3.zero;
        cameraPivot.localRotation = Quaternion.identity;
        transform.SetParent(cameraPivot);
        transform.localPosition = new Vector3(0f, 1.8f, 5f);
        transform.LookAt(player.position + new Vector3(0f, 1.2f, 0f));
        isCameraInitialized = true;
    }
}
