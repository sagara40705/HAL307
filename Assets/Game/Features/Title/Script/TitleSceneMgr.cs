using UnityEngine;

public class TitleSceneMgr : MonoBehaviour
{
    [SerializeField] private string _nextSceneName = "GameScene";
    [SerializeField] private UnityEngine.InputSystem.Key _key = UnityEngine.InputSystem.Key.None;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        // デバッグ用のスペースキーで次のシーンに遷移
        if (DebugShortcuts.Pressed(_key))
        {
            PauseManager.Instance.OpenPause();
            //SceneLoader.Load(_nextSceneName);
        }

        if (DebugShortcuts.Pressed(UnityEngine.InputSystem.Key.LeftCtrl))
        {
            PauseManager.Instance.ClosePause();
            //SceneLoader.Load(_nextSceneName);
        }

    }
}
