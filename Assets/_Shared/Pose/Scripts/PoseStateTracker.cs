using UnityEngine;

public class PoseStateTracker : MonoBehaviour
{
    private int _posingCount;

    public bool IsAnyPosing => _posingCount > 0;

    public void BeginPose()
    {
        _posingCount++;
    }

    public void EndPose()
    {
        _posingCount = Mathf.Max(0, _posingCount - 1);
    }
}
