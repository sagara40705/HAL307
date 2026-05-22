using System.Collections;
using System.Collections.Generic;
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
            SceneLoader sceneLoader = SceneLoader.Instance;
            Debug.AssertFormat(sceneLoader != null, "SceneLoader instance is null");
            StartCoroutine(LoadAllScenesCoroutine(sceneLoader));
        }

        private IEnumerator LoadAllScenesCoroutine(SceneLoader sceneLoader)
        {
            foreach (var sceneName in _scenes)
            {
                if (!string.IsNullOrEmpty(sceneName))
                {
                    yield return StartCoroutine(sceneLoader.LoadAdditiveAsync(sceneName));
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
