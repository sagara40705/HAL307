using UnityEngine;
using UnityEngine.Playables;

public sealed class CutsceneTimelineController : MonoBehaviour
{
    [SerializeField] private PlayableDirector _director;
    [SerializeField] private bool _stopOnAwake = true;
    [SerializeField] private bool _evaluateOnStop = true;

    public PlayableDirector Director => _director;

    private void Awake()
    {
        ResolveDirector();

        if (!_stopOnAwake)
        {
            return;
        }

        Stop();
    }

    public void Play()
    {
        ResolveDirector();
        if (_director == null)
        {
            Debug.LogWarning("[CutsceneTimelineController] Play skipped. Reason=PlayableDirector is null.");
            return;
        }

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

    public void Stop()
    {
        ResolveDirector();
        if (_director == null)
        {
            return;
        }

        _director.Stop();
        _director.time = 0d;

        if (_evaluateOnStop)
        {
            _director.Evaluate();
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
}
