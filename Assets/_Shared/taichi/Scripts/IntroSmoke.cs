using UnityEngine;

public class IntroSmoke : MonoBehaviour
{
    [SerializeField] GameObject _normalsmoke;
    [SerializeField] GameObject _storngsmoke;
    private bool _endlessEntranceActive;

    // エンドレスでは、キャラクターが表示されるフレームから粒子を見せる必要がある。
    // 共通のグランプリ用イベントは、従来どおり再生する。
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
            // 子の二重シミュレーションを避けるため、各システムを個別に再開する。短い事前再生で
            // 発生直後の粒子が空のフレームを埋め、キャラクターの表示は遅らせない。
            particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            particles.Simulate(0.35f, false, true, true);
            particles.Play(false);
        }
    }

    public void InstanceSmoke()
    {
        // 既存の開始Actionも呼ばれるが、蒸気を二重に発生させない。
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
