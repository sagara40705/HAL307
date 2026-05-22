using UnityEngine;
using UnityEngine.SceneManagement;

namespace Context
{
    public abstract class ContextBase : MonoBehaviour
    {
        [SerializeField] private bool _onlyMyScene;
        [SerializeField] private bool _initalize;
        protected bool OnlyMyScene => _onlyMyScene;

        protected virtual void Awake()
        {
            if (_onlyMyScene) { _initalize = true; }
            else { _initalize = false; }
        }

        protected static T FindContext<T>(string sceneName, string objectName) where T : ContextBase
        {
            Scene scene = SceneManager.GetSceneByName(sceneName);
            if (!scene.IsValid() || !scene.isLoaded) { Debug.LogWarning($"Scene'{sceneName}'が未ロードまたは無効"); return null; }

            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                if (root.name != objectName) { continue; }

                T context = root.GetComponent<T>();
                if (context != null) { return context; }
            }

            Debug.LogWarning(string.Format("Scene '{0}' にオブジェクト '{1}' が見つかりませんでした。", sceneName, objectName));
            return null;
        }
    }
}


