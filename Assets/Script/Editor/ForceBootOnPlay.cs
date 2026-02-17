using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public static class ForceBootOnPlay
{
#if UNITY_EDITOR
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void ForceBoot()
    {
        var active = SceneManager.GetActiveScene().name;
        if (active == BootConfig.BootSceneName) return;

        SceneManager.LoadScene(BootConfig.BootSceneName, LoadSceneMode.Single);
        Debug.Log($"[ForceBootOnPlay] '{active}' -> '{BootConfig.BootSceneName}'");
    }
#endif
}

