using UnityEngine;

public class IntroSmoke : MonoBehaviour
{
    [SerializeField] GameObject _particleSystem;
    [SerializeField] GameObject _storng;

    public void InstanceSmoke()
    {
        Debug.Log("Instance Smoke");
        _particleSystem.SetActive(true);
        _particleSystem.SetActive(true);
    }

    public void ClearSmoke()
    {
        Debug.Log("Clear Smoke");
        _particleSystem.SetActive(false);
        _storng.SetActive(false);
    }
}
