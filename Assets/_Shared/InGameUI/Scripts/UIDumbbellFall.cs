using UnityEngine;

public class UIDumbbellFall : MonoBehaviour
{
    [Header("落下設定")]
    [SerializeField] private float fallSpeed = 1800f;
    [SerializeField] private float rotateSpeed = 360f;

    [Header("揺れ設定")]
    [SerializeField] private float swayPower = 20f;
    [SerializeField] private float swaySpeed = 8f;

    private RectTransform rt;

    private Vector2 targetPosition;
    private bool isFallingToTarget;
    private bool isFallingOut;

    private float randomSeed;
    private float rotateDirection;
    private float outSpeed;
    private float outAcceleration;
    private float outTargetY;
    private float outDriftX;

    public bool IsMoving => isFallingToTarget || isFallingOut;

    private void Awake()
    {
        rt = GetComponent<RectTransform>();
    }

    private void Update()
    {
        if (isFallingToTarget)
        {
            UpdateFallToTarget();
        }

        if (isFallingOut)
        {
            UpdateFallOut();
        }
    }

    private void UpdateFallToTarget()
    {
        Vector2 pos = rt.anchoredPosition;

        float sway = Mathf.Sin(Time.time * swaySpeed + randomSeed) * swayPower;

        pos.y -= fallSpeed * Time.deltaTime;
        pos.x = Mathf.Lerp(pos.x, targetPosition.x + sway, Time.deltaTime * 8f);

        if (pos.y <= targetPosition.y)
        {
            pos = targetPosition;
            isFallingToTarget = false;
        }

        rt.anchoredPosition = pos;

        rt.Rotate(0f, 0f, rotateSpeed * rotateDirection * Time.deltaTime);
    }

    private void UpdateFallOut()
    {
        Vector2 pos = rt.anchoredPosition;

        outSpeed += outAcceleration * Time.deltaTime;

        pos.y -= outSpeed * Time.deltaTime;
        pos.x += outDriftX * Time.deltaTime;

        rt.anchoredPosition = pos;

        rt.Rotate(0f, 0f, rotateSpeed * rotateDirection * Time.deltaTime);

        if (pos.y <= outTargetY)
        {
            isFallingOut = false;
            gameObject.SetActive(false);
        }
    }

    public void StartFall(Vector2 startPosition, Vector2 targetPosition, float speed, float rotationSpeed)
    {
        if (rt == null)
        {
            rt = GetComponent<RectTransform>();
        }

        this.targetPosition = targetPosition;
        fallSpeed = speed;
        rotateSpeed = rotationSpeed;

        randomSeed = Random.Range(0f, 100f);
        rotateDirection = Random.value < 0.5f ? -1f : 1f;

        rt.anchoredPosition = startPosition;

        isFallingOut = false;
        isFallingToTarget = true;

        gameObject.SetActive(true);
    }

    public void StartFallOut(float targetY, float startSpeed, float acceleration, float driftX, float rotationSpeed)
    {
        if (rt == null)
        {
            rt = GetComponent<RectTransform>();
        }

        outTargetY = targetY;
        outSpeed = startSpeed;
        outAcceleration = acceleration;
        outDriftX = driftX;
        rotateSpeed = rotationSpeed;
        rotateDirection = Random.value < 0.5f ? -1f : 1f;

        isFallingToTarget = false;
        isFallingOut = true;
    }
}
