using System;
using UnityEngine;

[Serializable]
public sealed class WaveData
{
    [SerializeField, Min(0)] private int targetScore;
    [SerializeField] private InGamePresentationAction waveStartAction;
    [SerializeField] private InGamePresentationAction waveTransitionAction;

    public int TargetScore => targetScore;
    public InGamePresentationAction WaveStartAction => waveStartAction;
    public InGamePresentationAction WaveTransitionAction => waveTransitionAction;
}
