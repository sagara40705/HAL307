using System.Collections;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;

public sealed class CutsceneTimelineController : MonoBehaviour
{
    [SerializeField] private PlayableDirector _director;
    [SerializeField] private GameObject _contentRoot;
    [SerializeField] private bool _stopOnAwake = true;
    [SerializeField] private bool _evaluateOnStop = true;
    [SerializeField] private bool _hideContentOnAwake = true;
    [SerializeField] private bool _hideContentOnStop = true;

    private bool _isEnding;
    private bool _isStopping;

    public PlayableDirector Director => _director;

    private void Awake()
    {
        ResolveDirector();

        if (!_stopOnAwake)
        {
            if (_hideContentOnAwake)
            {
                SetContentActive(false);
            }

            return;
        }

        Stop();
    }

    private void OnEnable()
    {
        ResolveDirector();
        if (_director == null)
        {
            return;
        }

        _director.stopped += OnDirectorStopped;
    }

    private void OnDisable()
    {
        if (_director == null)
        {
            return;
        }

        _director.stopped -= OnDirectorStopped;
    }

    public void Play()
    {
        ResolveDirector();
        if (_director == null)
        {
            Debug.LogWarning("[CutsceneTimelineController] Play skipped. Reason=PlayableDirector is null.");
            return;
        }

        SetContentActive(true);
        _isEnding = false;
        _director.Play();
    }

    public void PlayFromStart()
    {
        ResolveDirector();
        if (_director == null)
        {
            Debug.LogWarning("[CutsceneTimelineController] PlayFromStart skipped. Reason=PlayableDirector is null.");
            return;
        }

        SetContentActive(true);
        _isEnding = false;
        _director.time = 0d;
        _director.Evaluate();
        _director.Play();
    }

    public void Pause()
    {
        ResolveDirector();
        if (_director == null)
        {
            return;
        }

        _director.Pause();
    }

    public void Resume()
    {
        Play();
    }

    public void EndCutScene()
    {
        if (_isEnding)
        {
            return;
        }

        _isEnding = true;
        Stop();
        SetContentActive(false);
        UnloadOwnerScene();
    }

    public void Stop()
    {
        ResolveDirector();
        if (_director == null)
        {
            return;
        }

        _isStopping = true;
        _director.Stop();
        _isStopping = false;

        _director.time = 0d;
        if (_evaluateOnStop)
        {
            _director.Evaluate();
        }

        if (_hideContentOnStop)
        {
            SetContentActive(false);
        }
    }

    private void ResolveDirector()
    {
        if (_director != null)
        {
            return;
        }

        _director = GetComponent<PlayableDirector>();
    }

    private void UnloadOwnerScene()
    {
        Scene ownerScene = gameObject.scene;
        if (!ownerScene.IsValid() || !ownerScene.isLoaded)
        {
            Debug.LogWarning("[CutsceneTimelineController] EndCutScene skipped unload. Reason=Owner scene is invalid or unloaded.");
            return;
        }

        SceneLoader sceneLoader = SceneLoader.Instance;
        if (sceneLoader != null && sceneLoader.IsInitialized && SceneLoader.Unload(ownerScene.name))
        {
            return;
        }

        StartCoroutine(UnloadOwnerSceneWithoutSceneLoader(ownerScene));
    }

    private IEnumerator UnloadOwnerSceneWithoutSceneLoader(Scene ownerScene)
    {
        if (!ownerScene.IsValid() || !ownerScene.isLoaded)
        {
            yield break;
        }

        if (SceneManager.sceneCount <= 1)
        {
            Debug.LogWarning($"[CutsceneTimelineController] EndCutScene skipped unload. Reason=Cannot unload the last scene. Scene='{ownerScene.name}'");
            yield break;
        }

        AsyncOperation operation = SceneManager.UnloadSceneAsync(ownerScene);
        if (operation == null)
        {
            Debug.LogWarning($"[CutsceneTimelineController] EndCutScene failed unload. Reason=UnloadSceneAsync returned null. Scene='{ownerScene.name}'");
            yield break;
        }

        yield return operation;
    }

    private void OnDirectorStopped(PlayableDirector director)
    {
        if (director != _director || _isStopping || !_hideContentOnStop)
        {
            return;
        }

        SetContentActive(false);
    }

    private void SetContentActive(bool active)
    {
        if (_contentRoot == null || _contentRoot.activeSelf == active)
        {
            return;
        }

        _contentRoot.SetActive(active);
    }
}
