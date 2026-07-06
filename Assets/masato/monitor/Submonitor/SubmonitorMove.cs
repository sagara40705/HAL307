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

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        GameObject enemyLObject = GameObject.FindGameObjectWithTag("EnemyL");
        GameObject enemyRObject = GameObject.FindGameObjectWithTag("EnemyR");
        
        if (enemyLObject != null)
        {
            enemyL = enemyLObject.transform;
        }

        if (enemyRObject != null)
        {
            enemyR = enemyRObject.transform;
        }

        if (playerObject == null || enemyL == null || enemyR == null)
        {
            return;
        }

        player = playerObject.transform;
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
    }

    void Update()
    {
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
            cameraPivot.Rotate(0f, moveSpeed * Time.deltaTime, 0f);
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
}
