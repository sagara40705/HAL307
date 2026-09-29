using System;
using System.Collections;
using System.Collections.Generic;

// 入れ子の例外を呼び出し元へ伝え、finallyによる状態解除まで実行する。
public static class CoroutineUtility
{
    public static IEnumerator Run(IEnumerator routine)
    {
        if (routine == null)
        {
            yield break;
        }

        var routines = new Stack<IEnumerator>();
        routines.Push(routine);
        try
        {
            while (routines.Count > 0)
            {
                IEnumerator current = routines.Peek();
                if (!current.MoveNext())
                {
                    routines.Pop();
                    (current as IDisposable)?.Dispose();
                }
                else if (current.Current is IEnumerator nested)
                {
                    routines.Push(nested);
                }
                else
                {
                    yield return current.Current;
                }
            }
        }
        finally
        {
            DisposeRemaining(routines);
        }
    }

    private static void DisposeRemaining(Stack<IEnumerator> routines)
    {
        if (routines.Count == 0)
        {
            return;
        }

        try
        {
            (routines.Pop() as IDisposable)?.Dispose();
        }
        finally
        {
            DisposeRemaining(routines);
        }
    }
}
