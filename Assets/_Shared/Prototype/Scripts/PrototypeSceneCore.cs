using UnityEngine;

namespace Prototype.Sandbox
{

    // プロトタイプsceneで統合するscene名をリストで管理
    // scene開始時にすべてadditiveで読み込む
    public class ProttypeSceneCore : MonoBehaviour
    {
        // 統合するsceneのリスト
        [SerializeField] private string[] _sceneNames;

        // scene開始時にすべてadditiveで読み込む
        void Start()
        {
            foreach (var sceneName in _sceneNames)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);
            }
        }
    }
}
