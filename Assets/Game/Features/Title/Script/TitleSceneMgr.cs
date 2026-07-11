using UnityEngine;

public class TitleSceneMgr : MonoBehaviour
{
    [SerializeField] private string _nextSceneName = "GameScene";
    [SerializeField] private UnityEngine.InputSystem.Key _key = UnityEngine.InputSystem.Key.None;

    //Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
    }

    //// Update is called once per frame
    void Update()
    {
        // デバッグ用のスペースキーで次のシーンに遷移
        if (DebugShortcuts.Pressed(_key))
        {
            SceneLoader.Load(_nextSceneName);
        }
    }

    public void OnVoiceCommand(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return;
        }
        command = command.Trim().ToLowerInvariant();
        if (command == "kettei")
        {
            StartLoad();
        }
    }

    private void StartLoad()
    {
        SceneLoader.Load(_nextSceneName);
    }

}
