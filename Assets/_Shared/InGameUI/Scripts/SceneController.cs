using UnityEngine;
using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;//UniTaskを使用するため

public class SceneController : MonoBehaviour
{
    public static SceneController Instance;

    [Header("シーン名")]
    public string titleSceneName = "TitleScene";
    public string gameSceneName = "GameScene";

    //現在使われてるシーン
    private string currentSceneName = "";

    private void Awake()
    {
        //シングルトンの実装
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        //最初はタイトルシーンを読み込む
        TransitionManager.Instance.StartDumbbellTransition(titleSceneName).Forget();
    }

    public async UniTask ChangeScene(string nextSceneName)
    {
        if (!string.IsNullOrEmpty(currentSceneName))
        {
            //現在のシーンがある場合はアンロードする
            await SceneManager.UnloadSceneAsync(currentSceneName);
        }

        //次のシーンを読み込む
        await SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Additive);

        //現在のシーン名を更新
        currentSceneName = nextSceneName;

        //加算ロードされたシーンをアクティブなシーンに設定する
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(nextSceneName))
;    
    }

    //外部から簡単に呼べるようにする
    public void GoToGame()
    {
        ChangeScene(gameSceneName).Forget();
    }

    public void GoToTitle()
    {
        ChangeScene(titleSceneName).Forget();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
