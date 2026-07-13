using UnityEngine;

public class IntroSmoke : MonoBehaviour
{
    [SerializeField] GameObject _normalsmoke;
    [SerializeField] GameObject _storngsmoke;

    public void InstanceSmoke()
    {
        Debug.Log("Instance Smoke");
        _normalsmoke.SetActive(true);
        _storngsmoke.SetActive(true);
    }

    public void ClearSmoke()
    {
        Debug.Log("Clear Smoke");
        _normalsmoke.SetActive(false);
        _storngsmoke.SetActive(false);
    }
}
