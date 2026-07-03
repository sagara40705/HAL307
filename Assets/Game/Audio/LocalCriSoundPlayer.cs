using System.Collections;
using CriWare;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CriAtomSource))]
[AddComponentMenu("Game/Audio/Local CRI Sound Player")]
public sealed class LocalCriSoundPlayer : MonoBehaviour
{
    [SerializeField] private CriAtomSource atomSource;

    [Header("Cue")]
    [SerializeField] private string cueSheet = "CueSheet_0";
    [SerializeField] private string cueName;

    [Header("Playback")]
    [SerializeField, Range(0f, 1f)] private float baseVolume = 1f;
    [SerializeField] private bool loop;
    [SerializeField] private bool playOnStart;

    [Header("Sound Manager")]
    [SerializeField] private bool applySoundManagerVolume = true;
    [SerializeField] private bool refreshVolumeWhilePlaying = true;

    private Coroutine _playCoroutine;
    private float _lastAppliedVolume = -1f;

    public string CueSheet => cueSheet;
    public string CueName => cueName;
    public float BaseVolume => baseVolume;

    private void Reset()
    {
        atomSource = GetComponent<CriAtomSource>();
    }

    private void Awake()
    {
        SetupSource();
    }

    private void Start()
    {
        if (playOnStart)
        {
            Play();
        }
    }

    private void Update()
    {
        if (!refreshVolumeWhilePlaying || atomSource == null)
        {
            return;
        }

        if (atomSource.status == CriAtomSourceBase.Status.Prep ||
            atomSource.status == CriAtomSourceBase.Status.Playing)
        {
            ApplyVolume();
        }
    }

    private void OnValidate()
    {
        atomSource = GetComponent<CriAtomSource>();
        baseVolume = Mathf.Clamp01(baseVolume);

        if (atomSource != null)
        {
            SetupSource();
            ApplyVolume();
        }
    }

    public void Play()
    {
        RequestPlay(cueName);
    }

    public void Play(string nextCueName)
    {
        SetCue(nextCueName);
        RequestPlay(cueName);
    }

    public void Stop()
    {
        if (_playCoroutine != null)
        {
            StopCoroutine(_playCoroutine);
            _playCoroutine = null;
        }

        if (atomSource == null)
        {
            return;
        }

        atomSource.Stop();
    }

    public void SetCue(string nextCueName)
    {
        cueName = nextCueName;

        if (atomSource != null)
        {
            atomSource.cueName = cueName;
        }
    }

    public void SetCueSheet(string nextCueSheet)
    {
        cueSheet = nextCueSheet;

        if (atomSource != null)
        {
            atomSource.cueSheet = cueSheet;
        }
    }

    public void SetBaseVolume(float volume)
    {
        baseVolume = Mathf.Clamp01(volume);
        ApplyVolume();
    }

    public void SetLoop(bool isLoop)
    {
        loop = isLoop;

        if (atomSource != null)
        {
            atomSource.loop = loop;
        }
    }

    public void ApplyVolume()
    {
        if (atomSource == null)
        {
            return;
        }

        float volume = GetEffectiveVolume();
        if (Mathf.Approximately(_lastAppliedVolume, volume))
        {
            return;
        }

        atomSource.volume = volume;
        _lastAppliedVolume = volume;
    }

    private bool SetupSource()
    {
        if (atomSource == null)
        {
            atomSource = GetComponent<CriAtomSource>();
        }

        if (atomSource == null)
        {
            Debug.LogWarning($"[LocalCriSoundPlayer] CriAtomSource was not found. GameObject='{name}'");
            return false;
        }

        atomSource.cueSheet = cueSheet;
        atomSource.cueName = cueName;
        atomSource.loop = loop;
        atomSource.playOnStart = false;
        return true;
    }

    private void RequestPlay(string requestedCueName)
    {
        if (_playCoroutine != null)
        {
            StopCoroutine(_playCoroutine);
        }

        _playCoroutine = StartCoroutine(CoPlay(requestedCueName));
    }

    private IEnumerator CoPlay(string requestedCueName)
    {
        if (!SetupSource())
        {
            _playCoroutine = null;
            yield break;
        }

        if (string.IsNullOrWhiteSpace(requestedCueName))
        {
            Debug.LogWarning($"[LocalCriSoundPlayer] Cue name is empty. GameObject='{name}'");
            _playCoroutine = null;
            yield break;
        }

        if (string.IsNullOrWhiteSpace(cueSheet))
        {
            Debug.LogWarning($"[LocalCriSoundPlayer] Cue sheet is empty. GameObject='{name}'");
            _playCoroutine = null;
            yield break;
        }

        CriAtomCueSheet targetCueSheet = FindCueSheet();
        if (targetCueSheet == null)
        {
            Debug.LogWarning(
                $"[LocalCriSoundPlayer] CRI cue sheet is not registered. CueSheet='{cueSheet}', GameObject='{name}'");
            _playCoroutine = null;
            yield break;
        }

        while (targetCueSheet.IsLoading)
        {
            yield return null;
        }

        if (targetCueSheet.IsError)
        {
            Debug.LogWarning(
                $"[LocalCriSoundPlayer] CRI cue sheet failed to load. CueSheet='{cueSheet}', GameObject='{name}'");
            _playCoroutine = null;
            yield break;
        }

        cueName = requestedCueName;
        atomSource.cueName = cueName;
        ApplyVolume();
        atomSource.Play();

        _playCoroutine = null;
    }

    private CriAtomCueSheet FindCueSheet()
    {
        CriAtom atom = UnityEngine.Object.FindFirstObjectByType<CriAtom>();
        if (atom == null)
        {
            Debug.LogWarning($"[LocalCriSoundPlayer] Active CriAtom component was not found. GameObject='{name}'");
            return null;
        }

        return atom.GetCueSheetInternal(cueSheet);
    }

    private float GetEffectiveVolume()
    {
        float volume = baseVolume;

        if (applySoundManagerVolume && SoundManager.TryGetInstance(out SoundManager soundManager))
        {
            volume *= soundManager.MasterVolume;
            volume *= soundManager.SeVolume;

            if (soundManager.IsMuted)
            {
                volume = 0f;
            }
        }

        return Mathf.Clamp01(volume);
    }
}
