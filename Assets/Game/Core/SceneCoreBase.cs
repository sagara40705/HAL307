using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SceneCoreBase : MonoBehaviour
{
    [SerializeField] private List<string> _loadSceneNames;

    protected virtual void Start()
    {
        SceneLoader sceneLoader = SceneLoader.Instance;
        Debug.AssertFormat(sceneLoader != null, "SceneLoader instance is null");
        StartCoroutine(LoadAllScenesCoroutine(sceneLoader));
    }

    protected IEnumerator LoadAllScenesCoroutine(SceneLoader sceneLoader)
    {
        foreach (var sceneName in _loadSceneNames)
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
