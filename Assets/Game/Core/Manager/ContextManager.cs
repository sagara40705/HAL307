using System.Collections;
using System.Collections.Generic;
using Context;
using UnityEngine;

public sealed class ContextManager : AppManagerBase<ContextManager>
{
    private readonly Dictionary<string, ContextBase> _contexts = new Dictionary<string, ContextBase>();

    protected override IEnumerator OnInitialize()
    {
        _contexts.Clear();
        yield break;
    }

    public void Register(ContextBase context)
    {
        if (context == null)
        {
            Debug.LogWarning("[ContextManager] Tried to register null context.");
            return;
        }

        string contextName = context.ContextName;
        if (string.IsNullOrWhiteSpace(contextName))
        {
            Debug.LogWarning($"[ContextManager] Context name is empty. Context={context.GetType().Name}");
            return;
        }

        if (_contexts.TryGetValue(contextName, out ContextBase existing))
        {
            if (ReferenceEquals(existing, context))
            {
                return;
            }

            Debug.LogError(
                $"[ContextManager] Duplicate context name '{contextName}'. " +
                $"Existing={existing.GetType().Name}, New={context.GetType().Name}. Replacing with the newer context.");
        }

        _contexts[contextName] = context;
    }

    public void Unregister(ContextBase context)
    {
        if (context == null)
        {
            return;
        }

        string contextName = context.ContextName;
        if (string.IsNullOrWhiteSpace(contextName))
        {
            return;
        }

        if (!_contexts.TryGetValue(contextName, out ContextBase existing))
        {
            return;
        }

        if (ReferenceEquals(existing, context))
        {
            _contexts.Remove(contextName);
        }
    }

    public bool TryGet<T>(string contextName, out T context) where T : ContextBase
    {
        context = null;

        if (string.IsNullOrWhiteSpace(contextName))
        {
            Debug.LogWarning($"[ContextManager] Context name is empty. Type={typeof(T).Name}");
            return false;
        }

        if (!_contexts.TryGetValue(contextName, out ContextBase found) || found == null)
        {
            Debug.LogWarning($"[ContextManager] Context not found. Name='{contextName}', Type={typeof(T).Name}");
            return false;
        }

        if (found is T typedContext)
        {
            context = typedContext;
            return true;
        }

        Debug.LogError(
            $"[ContextManager] Context type mismatch. " +
            $"Name='{contextName}', Expected={typeof(T).Name}, Actual={found.GetType().Name}");
        return false;
    }

    public T Get<T>(string contextName) where T : ContextBase
    {
        return TryGet(contextName, out T context) ? context : null;
    }
}
