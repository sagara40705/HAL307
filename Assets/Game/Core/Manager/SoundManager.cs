using System.Collections;
using System.Collections.Generic;
using CriWare;
using UnityEngine;

public sealed class SoundManager : AppManagerBase<SoundManager>
{
    private enum CriAcbState
    {
        Ready,
        AtomMissing,
        CueSheetMissing,
        CueSheetLoading,
        CueSheetError,
    }

    private const string MasterVolumeKey = "Sound.MasterVolume";
    private const string BgmVolumeKey = "Sound.BgmVolume";
    private const string SeVolumeKey = "Sound.SeVolume";
    private const string MuteKey = "Sound.Mute";

    [Header("Catalog")]
    [SerializeField] private SoundCatalog catalog;
    [SerializeField] private string resourcesCatalogPath = "Audio/SoundCatalog";

    [Header("CRI")]
    [SerializeField] private string cueSheetName = "CueSheet_0";
    [SerializeField] private bool validateCueNames = true;
    [SerializeField, Min(0f)] private float criAtomReadyTimeoutSeconds = 5f;

    [Header("Runtime Settings")]
    [SerializeField, Min(1)] private int seSourcePoolSize = 8;
    [SerializeField] private bool saveVolumeSettings = true;

    private readonly Dictionary<BgmId, BgmEntry> _bgmMap = new Dictionary<BgmId, BgmEntry>();
    private readonly Dictionary<SeId, SeEntry> _seMap = new Dictionary<SeId, SeEntry>();

    private CriAtomExAcb _atomExAcb;
    private CriAtomExPlayer _bgmPlayer;
    private CriAtomExPlayback _bgmPlayback;
    private CriAtomExPlayer[] _sePlayers;
    private CriAtomExPlayback[] _sePlaybacks;
    private float[] _seBaseVolumes;
    private int _seSourceIndex;
    private CriAcbState _lastCriAcbState = CriAcbState.AtomMissing;

    private Coroutine _bgmFadeCoroutine;

    private bool _hasCurrentBgm;
    private BgmId _currentBgmId = BgmId.None;
    private float _currentBgmBaseVolume = 1f;
    private float _currentBgmFade01 = 1f;

    private float _masterVolume = 1f;
    private float _bgmVolume = 1f;
    private float _seVolume = 1f;
    private bool _isMuted;
    private bool _isCriAvailable;
    private bool _isCriUnavailableLoggedOnPlayback;

    public float MasterVolume => _masterVolume;
    public float BgmVolume => _bgmVolume;
    public float SeVolume => _seVolume;
    public bool IsMuted => _isMuted;
    public bool HasCurrentBgm => _hasCurrentBgm;
    public BgmId CurrentBgmId => _currentBgmId;
    public bool IsCriAvailable => _isCriAvailable;

    // 初期化
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

        if (string.IsNullOrWhiteSpace(cueSheetName))
        {
            DisableCriSound("CRI cue sheet name is empty.");
        }
        else
        {
            yield return WaitForCriAtomAcb();

            if (_atomExAcb == null)
            {
                DisableCriSound(GetCriAcbFailureMessage(_lastCriAcbState));
            }
            else
            {
                _isCriAvailable = true;
                CreateCriPlayers();
            }
        }

        if (!BuildCatalogLookup())
        {
            FailInitialize("SoundCatalog contains invalid or duplicate entries.");
            yield break;
        }

        LoadVolumeSettings();
        ApplyAllRuntimeVolumes();
    }

    // BGMの再生
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

        if (!_bgmMap.TryGetValue(id, out BgmEntry entry))
        {
            Debug.LogWarning($"[SoundManager] BGM not found. Id={id}");
            return;
        }

        if (_hasCurrentBgm && _currentBgmId == id && IsBgmPlayerActive() && !restartIfSame)
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

    // BGMの停止
    public void StopBgm(float fadeTime = 0f)
    {
        if (_bgmPlayer == null)
        { return; }

        if (_bgmFadeCoroutine != null)
        {
            StopCoroutine(_bgmFadeCoroutine);
            _bgmFadeCoroutine = null;
        }

        if (!_hasCurrentBgm || !IsBgmPlayerActive())
        {
            _bgmPlayer.Stop();
            ClearCurrentBgmState();
            return;
        }

        if (fadeTime <= 0f)
        {
            _bgmPlayer.Stop();
            ClearCurrentBgmState();
            return;
        }

        _bgmFadeCoroutine = StartCoroutine(CoStopBgmWithFade(fadeTime));
    }


    // SEの再生
    public void PlaySe(SeId id, float volumeScale = 1f, float pitch = 1f)
    {
        if (!EnsureReady())
        { return; }

        if (id == SeId.None)
        { return; }

        if (!_seMap.TryGetValue(id, out SeEntry entry))
        {
            Debug.LogWarning($"[SoundManager] SE not found. Id={id}");
            return;
        }

        if (_sePlayers == null || _sePlayers.Length == 0)
        {
            Debug.LogWarning("[SoundManager] SE player pool is not created.");
            return;
        }

        int index = _seSourceIndex;
        _seSourceIndex = (_seSourceIndex + 1) % _sePlayers.Length;

        CriAtomExPlayer player = _sePlayers[index];
        float baseVolume = Mathf.Clamp01(entry.volume) * Mathf.Clamp01(volumeScale);

        _seBaseVolumes[index] = baseVolume;

        player.Stop(true);
        player.ResetParameters();
        player.SetCue(_atomExAcb, entry.CueName);
        player.SetVolume(GetEffectiveSePlayerVolume(baseVolume));
        player.SetPitch(PitchScaleToCents(pitch));
        player.Loop(false);
        _sePlaybacks[index] = player.Start();
    }

    // UIの再生
    public void PlayUi(SeId id)
    {
        PlaySe(id, 1f, 1f);
    }

    // マスターボリュームの設定

    public void SetMasterVolume(float volume01)
    {
        _masterVolume = Mathf.Clamp01(volume01);
        ApplyAllRuntimeVolumes();
        SaveVolumeSettingsIfNeeded();
    }

    // BGMボリュームの設定
    public void SetBgmVolume(float volume01)
    {
        _bgmVolume = Mathf.Clamp01(volume01);
        ApplyAllRuntimeVolumes();
        SaveVolumeSettingsIfNeeded();
    }

    // SEボリュームの設定
    public void SetSeVolume(float volume01)
    {
        _seVolume = Mathf.Clamp01(volume01);
        ApplyAllRuntimeVolumes();
        SaveVolumeSettingsIfNeeded();
    }

    // ミュートの設定
    public void SetMute(bool mute)
    {
        _isMuted = mute;
        ApplyAllRuntimeVolumes();
        SaveVolumeSettingsIfNeeded();
    }

    protected override void OnApplicationQuit()
    {
        DisposeCriPlayers();
        base.OnApplicationQuit();
    }

    protected override void OnDestroy()
    {
        DisposeCriPlayers();
        base.OnDestroy();
    }

    private void CreateCriPlayers()
    {
        if (_bgmPlayer == null)
        {
            _bgmPlayer = new CriAtomExPlayer();
        }

        if (_sePlayers != null && _sePlayers.Length > 0)
        { return; }

        int poolSize = Mathf.Max(1, seSourcePoolSize);

        _sePlayers = new CriAtomExPlayer[poolSize];
        _sePlaybacks = new CriAtomExPlayback[poolSize];
        _seBaseVolumes = new float[poolSize];

        for (int i = 0; i < poolSize; i++)
        {
            _sePlayers[i] = new CriAtomExPlayer();
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

            if (string.IsNullOrWhiteSpace(entry.CueName))
            {
                Debug.LogWarning($"[SoundManager] BGM cue name is empty. Id={entry.id}");
                continue;
            }

            if (_bgmMap.ContainsKey(entry.id))
            {
                Debug.LogError($"[SoundManager] Duplicate BGM id found. Id={entry.id}");
                return false;
            }

            ValidateCueName(entry.CueName, $"BGM {entry.id}");
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

            if (string.IsNullOrWhiteSpace(entry.CueName))
            {
                Debug.LogWarning($"[SoundManager] SE cue name is empty. Id={entry.id}");
                continue;
            }

            if (_seMap.ContainsKey(entry.id))
            {
                Debug.LogError($"[SoundManager] Duplicate SE id found. Id={entry.id}");
                return false;
            }

            ValidateCueName(entry.CueName, $"SE {entry.id}");
            _seMap.Add(entry.id, entry);
        }

        return true;
    }

    private void ValidateCueName(string cueName, string label)
    {
        if (!validateCueNames)
        {
            return;
        }

        if (_atomExAcb == null || _atomExAcb.Exists(cueName))
        {
            return;
        }

        Debug.LogWarning($"[SoundManager] CRI cue was not found. {label}, CueName='{cueName}'");
    }

    private IEnumerator WaitForCriAtomAcb()
    {
        float timer = 0f;
        _lastCriAcbState = CriAcbState.AtomMissing;

        while (true)
        {
            CriAcbState state = TryGetCriAtomAcb(cueSheetName, out _atomExAcb);
            _lastCriAcbState = state;
            if (state == CriAcbState.Ready)
            {
                yield break;
            }

            bool canWait = state == CriAcbState.CueSheetLoading;
            if (!canWait || timer >= criAtomReadyTimeoutSeconds)
            {
                yield break;
            }

            timer += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private void DisableCriSound(string reason)
    {
        _isCriAvailable = false;
        _atomExAcb = null;
        DisposeCriPlayers();
        Debug.LogWarning($"[SoundManager] CRI sound is disabled. {reason}");
    }

    private static CriAcbState TryGetCriAtomAcb(string targetCueSheetName, out CriAtomExAcb acb)
    {
        acb = null;

        CriAtom atom = UnityEngine.Object.FindFirstObjectByType<CriAtom>();
        if (atom == null)
        {
            return CriAcbState.AtomMissing;
        }

        CriAtomCueSheet cueSheet = atom.GetCueSheetInternal(targetCueSheetName);
        if (cueSheet == null)
        {
            return CriAcbState.CueSheetMissing;
        }

        if (cueSheet.IsLoading)
        {
            return CriAcbState.CueSheetLoading;
        }

        if (cueSheet.IsError)
        {
            return CriAcbState.CueSheetError;
        }

        acb = cueSheet.acb;
        return acb != null ? CriAcbState.Ready : CriAcbState.CueSheetError;
    }

    private string GetCriAcbFailureMessage(CriAcbState state)
    {
        switch (state)
        {
            case CriAcbState.AtomMissing:
                return
                    "Active CriAtom component was not found. " +
                    $"Add a CRIWARE/CRI Atom object to Boot scene and register CueSheetName='{cueSheetName}'.";
            case CriAcbState.CueSheetMissing:
                return
                    $"CRI cue sheet is not registered. CueSheetName='{cueSheetName}'. " +
                    "Set the same cue sheet name on the CriAtom component.";
            case CriAcbState.CueSheetLoading:
                return
                    $"CRI cue sheet loading timed out. CueSheetName='{cueSheetName}', " +
                    $"Timeout={criAtomReadyTimeoutSeconds:0.###} sec.";
            case CriAcbState.CueSheetError:
                return
                    $"CRI cue sheet failed to load. CueSheetName='{cueSheetName}'. " +
                    "Check the ACB/AWB file paths and StreamingAssets files on the CriAtom component.";
            default:
                return $"CRI cue sheet is not ready. CueSheetName='{cueSheetName}'.";
        }
    }

    private void StartBgmImmediate(BgmEntry entry)
    {
        _currentBgmId = entry.id;
        _currentBgmBaseVolume = Mathf.Clamp01(entry.volume);
        _currentBgmFade01 = 1f;
        _hasCurrentBgm = true;

        _bgmPlayer.Stop();
        _bgmPlayer.ResetParameters();
        _bgmPlayer.SetCue(_atomExAcb, entry.CueName);
        _bgmPlayer.SetVolume(GetEffectiveBgmPlayerVolume(_currentBgmBaseVolume, _currentBgmFade01));
        _bgmPlayer.Loop(true);
        _bgmPlayback = _bgmPlayer.Start();
    }

    private IEnumerator CoPlayBgmWithFade(BgmEntry nextEntry, float fadeTime)
    {
        if (_hasCurrentBgm && IsBgmPlayerActive())
        {
            yield return FadeCurrentBgm(0f, fadeTime);
            _bgmPlayer.Stop();
        }

        _currentBgmId = nextEntry.id;
        _currentBgmBaseVolume = Mathf.Clamp01(nextEntry.volume);
        _currentBgmFade01 = 0f;
        _hasCurrentBgm = true;

        _bgmPlayer.ResetParameters();
        _bgmPlayer.SetCue(_atomExAcb, nextEntry.CueName);
        _bgmPlayer.SetVolume(GetEffectiveBgmPlayerVolume(_currentBgmBaseVolume, _currentBgmFade01));
        _bgmPlayer.Loop(true);
        _bgmPlayback = _bgmPlayer.Start();

        yield return FadeCurrentBgm(1f, fadeTime);

        _bgmFadeCoroutine = null;
    }

    private IEnumerator CoStopBgmWithFade(float fadeTime)
    {
        yield return FadeCurrentBgm(0f, fadeTime);

        _bgmPlayer.Stop();
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
            ApplyBgmPlayerVolume();
            yield break;
        }

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(time / duration);

            _currentBgmFade01 = Mathf.Lerp(startFade, targetFade01, t);
            ApplyBgmPlayerVolume();

            yield return null;
        }

        _currentBgmFade01 = targetFade01;
        ApplyBgmPlayerVolume();
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

        if (!_isCriAvailable || _atomExAcb == null)
        {
            if (!_isCriUnavailableLoggedOnPlayback)
            {
                Debug.LogWarning("[SoundManager] Sound playback was skipped because CRI sound is disabled.");
                _isCriUnavailableLoggedOnPlayback = true;
            }
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
        ApplyBgmPlayerVolume();
        ApplySePlayerVolumes();
    }

    private void ApplyBgmPlayerVolume()
    {
        if (_bgmPlayer == null || !_hasCurrentBgm)
        {
            return;
        }

        _bgmPlayer.SetVolume(GetEffectiveBgmPlayerVolume(_currentBgmBaseVolume, _currentBgmFade01));
        if (IsBgmPlayerActive())
        {
            _bgmPlayer.Update(_bgmPlayback);
        }
    }

    private void ApplySePlayerVolumes()
    {
        if (_sePlayers == null)
        {
            return;
        }

        for (int i = 0; i < _sePlayers.Length; i++)
        {
            CriAtomExPlayer player = _sePlayers[i];
            if (player == null || !IsCriPlayerActive(player))
            {
                continue;
            }

            player.SetVolume(GetEffectiveSePlayerVolume(_seBaseVolumes[i]));
            player.Update(_sePlaybacks[i]);
        }
    }

    private float GetEffectiveBgmPlayerVolume(float baseVolume, float fade01)
    {
        float volume = Mathf.Clamp01(baseVolume) * Mathf.Clamp01(fade01);
        volume *= GetRuntimeMasterScalar();
        volume *= GetRuntimeChannelScalar(_bgmVolume);
        return Mathf.Clamp01(volume);
    }

    private float GetEffectiveSePlayerVolume(float baseVolume)
    {
        float volume = Mathf.Clamp01(baseVolume);
        volume *= GetRuntimeMasterScalar();
        volume *= GetRuntimeChannelScalar(_seVolume);
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

    private bool IsBgmPlayerActive()
    {
        return IsCriPlayerActive(_bgmPlayer);
    }

    private static bool IsCriPlayerActive(CriAtomExPlayer player)
    {
        if (player == null)
        {
            return false;
        }

        CriAtomExPlayer.Status status = player.GetStatus();
        return status == CriAtomExPlayer.Status.Prep || status == CriAtomExPlayer.Status.Playing;
    }

    private static float PitchScaleToCents(float pitchScale)
    {
        float clampedPitchScale = Mathf.Clamp(pitchScale, 0.1f, 3f);
        return Mathf.Log(clampedPitchScale) / Mathf.Log(2f) * 1200f;
    }

    private void DisposeCriPlayers()
    {
        if (_bgmFadeCoroutine != null)
        {
            StopCoroutine(_bgmFadeCoroutine);
            _bgmFadeCoroutine = null;
        }

        DisposePlayer(ref _bgmPlayer);

        if (_sePlayers != null)
        {
            for (int i = 0; i < _sePlayers.Length; i++)
            {
                DisposePlayer(ref _sePlayers[i]);
            }
        }

        _sePlayers = null;
        _sePlaybacks = null;
        _seBaseVolumes = null;
        _atomExAcb = null;
    }

    private static void DisposePlayer(ref CriAtomExPlayer player)
    {
        if (player == null)
        {
            return;
        }

        player.Stop(true);
        player.Dispose();
        player = null;
    }
}
