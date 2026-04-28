using System;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    Dictionary<PoseId, Action> poseActions;

    public PoseId currentPoseId;
    public float poseTimer = 0f;

    void Awake()
    {
        poseActions = new Dictionary<PoseId, Action>()
        {
            { PoseId.Pose_A, Pose_A },
            { PoseId.Pose_B, Pose_B },
            { PoseId.Pose_B, Pose_C },
        };
    }

    void Start()
    {
        currentPoseId = PoseId.None;
        poseTimer = 0f;
    }

    public void DoPose(PoseId type)
    {
        poseActions[type]?.Invoke();
    }

    void Pose_A() { Debug.Log("Pose_A"); }
    void Pose_B() { Debug.Log("Pose_B"); }
    void Pose_C() { Debug.Log("Pose_C"); }
}
