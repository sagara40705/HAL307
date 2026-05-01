using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public sealed class SoundManager : AppManagerBase<SoundManager>
{
    private const string MasterVolumeKey = "Sound.MasterVolume";
    private const string BgmVolumeKey = "Sound.BgmVolume";
    private const string SeVolumeKey = "Sound.SeVolume";
    private const string MuteKey = "Sound.Mute";

    [Header("Catalog")]
    [SerializeField] private SoundCatalog catalog;
    [SerializeField] private string resourcesCatalogPath = "Audio/SoundCatalog";

    [Header("Optional AudioMixer")]
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private string masterVolumeParameter = "MasterVolume";
    [SerializeField] private string bgmVolumeParameter = "BgmVolume";
    [SerializeField] private string seVolumeParameter = "SeVolume";

    [Header("Runtime Settings")]
    [SerializeField, Min(1)] private int seSourcePoolSize = 8;
    [SerializeField] private bool saveVolumeSettings = true;

    private readonly Dictionary<BgmId, BgmEntry> _bgmMap = new Dictionary<BgmId, BgmEntry>();
    private readonly Dictionary<SeId, SeEntry> _seMap = new Dictionary<SeId, SeEntry>();

    private AudioSource _bgmSource;
    private AudioSource[] _seSources;
    private float[] _seBaseVolumes;
    private int _seSourceIndex;

    private Coroutine _bgmFadeCoroutine;

    private bool _hasCurrentBgm;
    private BgmId _currentBgmId = BgmId.None;
    private float _currentBgmBaseVolume = 1f;
    private float _currentBgmFade01 = 1f;

    private float _masterVolume = 1f;
    private float _bgmVolume = 1f;
    private float _seVolume = 1f;
    private bool _isMuted;

    public float MasterVolume => _masterVolume;
    public float BgmVolume => _bgmVolume;
    public float SeVolume => _seVolume;
    public bool IsMuted => _isMuted;
    public bool HasCurrentBgm => _hasCurrentBgm;
    public BgmId CurrentBgmId => _currentBgmId;

    protected override void OnSingletonAwake()
    {
        CreateRuntimeAudioSources();
    }

    protected override IEnumerator OnInitialize()
    {
        if (catalog == null && !string.IsNullOrWhiteSpace(resourcesCatalogPath))
        {
            catalog = Resources.Load<SoundCatalog>(resourcesCatalogPath);
        }

        if (catalog == null)
        {
            FailInitialize(
                $"SoundCatalog is not assigned. " +
                $"Assign it in inspector, or place it under Resources/{resourcesCatalogPath}.asset");
            yield break;
        }

        if (!BuildCatalogLookup())
        {
            FailInitialize("SoundCatalog contains invalid or duplicate entries.");
            yield break;
        }

        LoadVolumeSettings();
        ApplyAllRuntimeVolumes();

        yield break;
    }

    public void PlayBgm(BgmId id, float fadeTime = 0f, bool restartIfSame = false)
    {
        if (!EnsureReady())
        {
            return;
        }

        if (id == BgmId.None)
        {
            StopBgm(fadeTime);
            return;
        }

        if (!_bgmMap.TryGetValue(id, out BgmEntry entry) || entry.clip == null)
        {
            Debug.LogWarning($"[SoundManager] BGM not found. Id={id}");
            return;
        }

        if (_hasCurrentBgm && _currentBgmId == id && _bgmSource.isPlaying && !restartIfSame)
        {
            return;
        }

        if (_bgmFadeCoroutine != null)
        {
            StopCoroutine(_bgmFadeCoroutine);
            _bgmFadeCoroutine = null;
        }

        if (fadeTime <= 0f)
        {
            StartBgmImmediate(entry);
            return;
        }

        _bgmFadeCoroutine = StartCoroutine(CoPlayBgmWithFade(entry, fadeTime));
    }

    public void StopBgm(float fadeTime = 0f)
    {
        if (_bgmSource == null)
        { return; }

        if (_bgmFadeCoroutine != null)
        {
            StopCoroutine(_bgmFadeCoroutine);
            _bgmFadeCoroutine = null;
        }

        if (!_bgmSource.isPlaying || !_hasCurrentBgm)
        {
            ClearCurrentBgmState();
            return;
        }

        if (fadeTime <= 0f)
        {
            _bgmSource.Stop();
            _bgmSource.clip = null;
            ClearCurrentBgmState();
            return;
        }

        _bgmFadeCoroutine = StartCoroutine(CoStopBgmWithFade(fadeTime));
    }

    public void PlaySe(SeId id, float volumeScale = 1f, float pitch = 1f)
    {
        if (!EnsureReady())
        { return; }

        if (id == SeId.None)
        { return; }

        if (!_seMap.TryGetValue(id, out SeEntry entry) || entry.clip == null)
        {
            Debug.LogWarning($"[SoundManager] SE not found. Id={id}");
            return;
        }

        if (_seSources == null || _seSources.Length == 0)
        {
            Debug.LogWarning("[SoundManager] SE source pool is not created.");
            return;
        }

        int index = _seSourceIndex;
        _seSourceIndex = (_seSourceIndex + 1) % _seSources.Length;

        AudioSource source = _seSources[index];
        float baseVolume = Mathf.Clamp01(entry.volume) * Mathf.Clamp01(volumeScale);

        _seBaseVolumes[index] = baseVolume;

        source.Stop();
        source.clip = entry.clip;
        source.loop = false;
        source.pitch = Mathf.Clamp(pitch, 0.1f, 3f);
        source.volume = GetEffectiveSeSourceVolume(baseVolume);
        source.Play();
    }

    public void PlayUi(SeId id)
    {
        PlaySe(id, 1f, 1f);
    }

    public void SetMasterVolume(float volume01)
    {
        _masterVolume = Mathf.Clamp01(volume01);
        ApplyAllRuntimeVolumes();
        SaveVolumeSettingsIfNeeded();
    }

    public void SetBgmVolume(float volume01)
    {
        _bgmVolume = Mathf.Clamp01(volume01);
        ApplyAllRuntimeVolumes();
        SaveVolumeSettingsIfNeeded();
    }

    public void SetSeVolume(float volume01)
    {
        _seVolume = Mathf.Clamp01(volume01);
        ApplyAllRuntimeVolumes();
        SaveVolumeSettingsIfNeeded();
    }

    public void SetMute(bool mute)
    {
        _isMuted = mute;
        ApplyAllRuntimeVolumes();
        SaveVolumeSettingsIfNeeded();
    }

    private void CreateRuntimeAudioSources()
    {
        if (_bgmSource == null)
        {
            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.playOnAwake = false;
            _bgmSource.loop = true;
            _bgmSource.spatialBlend = 0f;
        }

        if (_seSources != null && _seSources.Length > 0)
        { return; }

        int poolSize = Mathf.Max(1, seSourcePoolSize);

        _seSources = new AudioSource[poolSize];
        _seBaseVolumes = new float[poolSize];

        for (int i = 0; i < poolSize; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0f;

            _seSources[i] = source;
            _seBaseVolumes[i] = 1f;
        }
    }

    private bool BuildCatalogLookup()
    {
        _bgmMap.Clear();
        _seMap.Clear();

        foreach (BgmEntry entry in catalog.Bgms)
        {
            if (entry == null)
            {
                continue;
            }

            if (entry.id == BgmId.None)
            {
                Debug.LogWarning("[SoundManager] BGM entry with None was ignored.");
                continue;
            }

            if (entry.clip == null)
            {
                Debug.LogWarning($"[SoundManager] BGM clip is missing. Id={entry.id}");
                continue;
            }

            if (_bgmMap.ContainsKey(entry.id))
            {
                Debug.LogError($"[SoundManager] Duplicate BGM id found. Id={entry.id}");
                return false;
            }

            _bgmMap.Add(entry.id, entry);
        }

        foreach (SeEntry entry in catalog.Ses)
        {
            if (entry == null)
            {
                continue;
            }

            if (entry.id == SeId.None)
            {
                Debug.LogWarning("[SoundManager] SE entry with None was ignored.");
                continue;
            }

            if (entry.clip == null)
            {
                Debug.LogWarning($"[SoundManager] SE clip is missing. Id={entry.id}");
                continue;
            }

            if (_seMap.ContainsKey(entry.id))
            {
                Debug.LogError($"[SoundManager] Duplicate SE id found. Id={entry.id}");
                return false;
            }

            _seMap.Add(entry.id, entry);
        }

        return true;
    }

    private void StartBgmImmediate(BgmEntry entry)
    {
        _currentBgmId = entry.id;
        _currentBgmBaseVolume = Mathf.Clamp01(entry.volume);
        _currentBgmFade01 = 1f;
        _hasCurrentBgm = true;

        _bgmSource.Stop();
        _bgmSource.clip = entry.clip;
        _bgmSource.loop = true;
        _bgmSource.volume = GetEffectiveBgmSourceVolume(_currentBgmBaseVolume, _currentBgmFade01);
        _bgmSource.Play();
    }

    private IEnumerator CoPlayBgmWithFade(BgmEntry nextEntry, float fadeTime)
    {
        if (_hasCurrentBgm && _bgmSource.isPlaying)
        {
            yield return FadeCurrentBgm(0f, fadeTime);
        }

        _currentBgmId = nextEntry.id;
        _currentBgmBaseVolume = Mathf.Clamp01(nextEntry.volume);
        _currentBgmFade01 = 0f;
        _hasCurrentBgm = true;

        _bgmSource.Stop();
        _bgmSource.clip = nextEntry.clip;
        _bgmSource.loop = true;
        _bgmSource.volume = GetEffectiveBgmSourceVolume(_currentBgmBaseVolume, _currentBgmFade01);
        _bgmSource.Play();

        yield return FadeCurrentBgm(1f, fadeTime);

        _bgmFadeCoroutine = null;
    }

    private IEnumerator CoStopBgmWithFade(float fadeTime)
    {
        yield return FadeCurrentBgm(0f, fadeTime);

        _bgmSource.Stop();
        _bgmSource.clip = null;
        ClearCurrentBgmState();

        _bgmFadeCoroutine = null;
    }

    private IEnumerator FadeCurrentBgm(float targetFade01, float duration)
    {
        float startFade = _currentBgmFade01;
        float time = 0f;

        if (duration <= 0f)
        {
            _currentBgmFade01 = targetFade01;
            _bgmSource.volume = GetEffectiveBgmSourceVolume(_currentBgmBaseVolume, _currentBgmFade01);
            yield break;
        }

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);

            _currentBgmFade01 = Mathf.Lerp(startFade, targetFade01, t);
            _bgmSource.volume = GetEffectiveBgmSourceVolume(_currentBgmBaseVolume, _currentBgmFade01);

            yield return null;
        }

        _currentBgmFade01 = targetFade01;
        _bgmSource.volume = GetEffectiveBgmSourceVolume(_currentBgmBaseVolume, _currentBgmFade01);
    }

    private void ClearCurrentBgmState()
    {
        _hasCurrentBgm = false;
        _currentBgmId = BgmId.None;
        _currentBgmBaseVolume = 1f;
        _currentBgmFade01 = 1f;
    }

    private bool EnsureReady()
    {
        if (!IsInitialized)
        {
            Debug.LogWarning("[SoundManager] Not initialized yet.");
            return false;
        }

        return true;
    }

    private void LoadVolumeSettings()
    {
        _masterVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));
        _bgmVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(BgmVolumeKey, 1f));
        _seVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(SeVolumeKey, 1f));
        _isMuted = PlayerPrefs.GetInt(MuteKey, 0) != 0;
    }

    private void SaveVolumeSettingsIfNeeded()
    {
        if (!saveVolumeSettings)
        { return; }

        PlayerPrefs.SetFloat(MasterVolumeKey, _masterVolume);
        PlayerPrefs.SetFloat(BgmVolumeKey, _bgmVolume);
        PlayerPrefs.SetFloat(SeVolumeKey, _seVolume);
        PlayerPrefs.SetInt(MuteKey, _isMuted ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void ApplyAllRuntimeVolumes()
    {
        ApplyMixerVolumes();

        if (_bgmSource != null)
        {
            _bgmSource.volume = GetEffectiveBgmSourceVolume(_currentBgmBaseVolume, _currentBgmFade01);
        }

        if (_seSources == null)
        { return; }

        for (int i = 0; i < _seSources.Length; i++)
        {
            if (_seSources[i] == null)
            {
                continue;
            }

            if (!_seSources[i].isPlaying)
            {
                continue;
            }

            _seSources[i].volume = GetEffectiveSeSourceVolume(_seBaseVolumes[i]);
        }
    }

    private void ApplyMixerVolumes()
    {
        if (audioMixer == null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(masterVolumeParameter))
        {
            audioMixer.SetFloat(masterVolumeParameter, _isMuted ? -80f : Linear01ToDecibel(_masterVolume));
        }

        if (!string.IsNullOrWhiteSpace(bgmVolumeParameter))
        {
            audioMixer.SetFloat(bgmVolumeParameter, Linear01ToDecibel(_bgmVolume));
        }

        if (!string.IsNullOrWhiteSpace(seVolumeParameter))
        {
            audioMixer.SetFloat(seVolumeParameter, Linear01ToDecibel(_seVolume));
        }
    }

    private float GetEffectiveBgmSourceVolume(float baseVolume, float fade01)
    {
        float volume = Mathf.Clamp01(baseVolume) * Mathf.Clamp01(fade01);

        if (audioMixer == null)
        {
            volume *= GetRuntimeMasterScalar();
            volume *= GetRuntimeChannelScalar(_bgmVolume);
        }

        return Mathf.Clamp01(volume);
    }

    private float GetEffectiveSeSourceVolume(float baseVolume)
    {
        float volume = Mathf.Clamp01(baseVolume);

        if (audioMixer == null)
        {
            volume *= GetRuntimeMasterScalar();
            volume *= GetRuntimeChannelScalar(_seVolume);
        }

        return Mathf.Clamp01(volume);
    }

    private float GetRuntimeMasterScalar()
    {
        return _isMuted ? 0f : _masterVolume;
    }

    private float GetRuntimeChannelScalar(float channelVolume)
    {
        return _isMuted ? 0f : channelVolume;
    }

    private static float Linear01ToDecibel(float volume01)
    {
        if (volume01 <= 0.0001f)
        {
            return -80f;
        }

        return Mathf.Log10(volume01) * 20f;
    }
}
