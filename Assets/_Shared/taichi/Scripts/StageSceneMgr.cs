using UnityEngine;

public class StageSceneMgr : MonoBehaviour
{
    [SerializeField] private string _nextSceneName = "Result";

    public void GoResultScene()
    {
        SceneLoader.Load(_nextSceneName);
    }
}
