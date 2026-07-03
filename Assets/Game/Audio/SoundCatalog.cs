using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class BgmEntry
{
    public BgmId id;
    public string cueName;
    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = 1f;

    public string CueName => string.IsNullOrWhiteSpace(cueName) ? id.ToString() : cueName;
}

[Serializable]
public sealed class SeEntry
{
    public SeId id;
    public string cueName;
    public AudioClip clip;

    [Range(0f, 1f)]
    public float volume = 1f;

    public string CueName => string.IsNullOrWhiteSpace(cueName) ? id.ToString() : cueName;
}

[CreateAssetMenu(menuName = "Game/Audio/SoundCatalog", fileName = "SoundCatalog")]
public sealed class SoundCatalog : ScriptableObject
{
    [SerializeField] private List<BgmEntry> bgms = new List<BgmEntry>();
    [SerializeField] private List<SeEntry> ses = new List<SeEntry>();

    public IReadOnlyList<BgmEntry> Bgms => bgms;
    public IReadOnlyList<SeEntry> Ses => ses;
}
