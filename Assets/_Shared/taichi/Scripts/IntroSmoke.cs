using UnityEngine;

public class IntroSmoke : MonoBehaviour
{
    [SerializeField] GameObject _normalsmoke;
    [SerializeField] GameObject _storngsmoke;
    private bool _endlessEntranceActive;

    // Endless needs visible particles in the same frame that its actors are revealed.
    // Keep the shared Grand Prix event's normal playback unchanged.
    public void BeginEndlessEntrance()
    {
        if (_endlessEntranceActive) { return; }
        _endlessEntranceActive = true;
        RestartVisibleSmoke(_normalsmoke);
        RestartVisibleSmoke(_storngsmoke);
    }

    private static void RestartVisibleSmoke(GameObject root)
    {
        if (root == null) { return; }
        root.SetActive(true);
        foreach (ParticleSystem particles in root.GetComponentsInChildren<ParticleSystem>())
        {
            // Restart one system at a time to avoid simulating children twice. The short
            // pre-roll fills the emitter's initially empty frame without delaying the actors.
            particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.Simulate(0.35f, false, true, true);
            particles.Play(false);
        }
    }

    public void InstanceSmoke()
    {
        // The existing intro Action is still invoked, but must not start a second burst.
        if (_endlessEntranceActive) { return; }
        Debug.Log("Instance Smoke");
        _normalsmoke.SetActive(true);
        _storngsmoke.SetActive(true);
    }

    public void ClearSmoke()
    {
        Debug.Log("Clear Smoke");
        if (_endlessEntranceActive)
        {
            foreach (ParticleSystem particles in GetComponentsInChildren<ParticleSystem>(true))
            {
                particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            _endlessEntranceActive = false;
        }
        if (_normalsmoke != null) { _normalsmoke.SetActive(false); }
        if (_storngsmoke != null) { _storngsmoke.SetActive(false); }
    }
}
