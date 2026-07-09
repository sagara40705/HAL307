using UnityEngine;

public class StageProgressProvider : MonoBehaviour
{
    public static StageProgressProvider Instance { get; private set; }

    [Header("距離計算用")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform endPoint;
    [SerializeField] private Transform currentPoint;

    [Header("到達判定")]
    [SerializeField] private float arriveDistance = 1f;

    public float GaugeRate { get; private set; } = 1f;
    public bool IsArrived { get; private set; } = false;

    private float totalDistance;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        if (startPoint == null || endPoint == null || currentPoint == null)
        {
            Debug.LogError("[StageProgressProvider] 参照が足りません。", this);
            return;
        }

        totalDistance = Vector3.Distance(
            startPoint.position,
            endPoint.position
        );

        if (totalDistance <= arriveDistance)
        {
            Debug.LogWarning(
                "[StageProgressProvider] StartPointとEndPointが近すぎます。",
                this
            );

            totalDistance = arriveDistance + 0.01f;
        }
    }

    private void Update()
    {
        if (startPoint == null || endPoint == null || currentPoint == null)
        {
            return;
        }

        float currentDistance = Vector3.Distance(
            currentPoint.position,
            endPoint.position
        );

        if (currentDistance <= arriveDistance)
        {
            GaugeRate = 0f;
            IsArrived = true;
            return;
        }

        GaugeRate = Mathf.InverseLerp(
            arriveDistance,
            totalDistance,
            currentDistance
        );

        GaugeRate = Mathf.Clamp01(GaugeRate);
        IsArrived = false;
    }
}
