using UnityEngine;

namespace Context
{
    [DefaultExecutionOrder(-1000)]
    public abstract class ContextBase : MonoBehaviour
    {
        [SerializeField] private bool _onlyMyScene;
        [SerializeField] private bool _initalize;
        [SerializeField] private string _contextName;

        public string ContextName => string.IsNullOrWhiteSpace(_contextName) ? gameObject.name : _contextName;
        protected bool OnlyMyScene => _onlyMyScene;

        protected virtual void Awake()
        {
            _initalize = _onlyMyScene;

            global::ContextManager manager = global::ContextManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning($"[{GetType().Name}] ContextManager is not initialized. ContextName='{ContextName}'");
                return;
            }

            manager.Register(this);
        }

        protected virtual void OnDestroy()
        {
            global::ContextManager manager = global::ContextManager.Instance;
            if (manager != null)
            {
                manager.Unregister(this);
            }
        }

        protected static T FindContext<T>(string contextName) where T : ContextBase
        {
            global::ContextManager manager = global::ContextManager.Instance;
            if (manager == null)
            {
                Debug.LogWarning($"[ContextBase] ContextManager is not initialized. ContextName='{contextName}', Type={typeof(T).Name}");
                return null;
            }

            return manager.Get<T>(contextName);
        }
    }
}
