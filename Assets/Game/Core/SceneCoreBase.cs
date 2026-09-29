using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SceneCoreBase : MonoBehaviour
{
    [SerializeField] private List<string> _loadSceneNames;

    protected virtual void Start()
    {
        SceneLoader sceneLoader = SceneLoader.Instance;
        StartCoroutine(LoadAllScenesCoroutine(sceneLoader));
    }

    protected IEnumerator LoadAllScenesCoroutine(SceneLoader sceneLoader)
    {
        return LoadAllScenesCoroutine(sceneLoader, _loadSceneNames);
    }

    protected IEnumerator LoadAllScenesCoroutine(SceneLoader sceneLoader, IEnumerable<string> sceneNames)
    {
        if (sceneLoader == null || !sceneLoader.IsInitialized)
        {
            Debug.LogError($"[{GetType().Name}] SceneLoader is not ready.");
            yield break;
        }

        if (sceneNames == null)
        {
            yield break;
        }

        foreach (string sceneName in sceneNames)
        {
            if (!string.IsNullOrWhiteSpace(sceneName))
            {
                yield return sceneLoader.LoadAdditiveAsync(sceneName);
                if (sceneLoader.IsSceneLoaded(sceneName))
                {
                    Debug.Log($"Loaded scene: {sceneName}");
                }
                else
                {
                    Debug.LogError($"[SceneCoreBase] Scene load did not complete. Scene='{sceneName}'");
                }
            }
            else
            {
                Debug.LogWarning("Scene name is empty or null.");
            }
        }
    }
}
