using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Prototype.Sandbox
{

    // プロトタイプsceneで統合するscene名をリストで管理
    // scene開始時にすべてadditiveで読み込む
    public class ProttypeSceneCore : MonoBehaviour
    {
        // 統合するsceneのリスト
        [SerializeField] private List<string> _scenes;

        // scene開始時にすべてadditiveで読み込む
        void Start()
        {
            foreach (var sceneName in _scenes)
            {
                if (!string.IsNullOrEmpty(sceneName))
                {
                    UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName, UnityEngine.SceneManagement.LoadSceneMode.Additive);
                    Debug.Log($"Loaded scene: {sceneName}");
                }
                else
                {
                    Debug.LogWarning("Scene name is empty or null.");
                }
            }
        }
    }
}
