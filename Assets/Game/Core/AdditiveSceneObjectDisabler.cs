using System.Collections.Generic;
using UnityEngine;

public class AdditiveSceneObjectDisabler : MonoBehaviour
{
    [SerializeField] private List<GameObject> _disableTargets = new();

    public void DisableTargets()
    {
        foreach (var obj in _disableTargets)
        {
            if (obj == null) { continue; }
            obj.SetActive(false);
        }
    }

    public void EnableTargets()
    {
        foreach (var obj in _disableTargets)
        {
            if (obj == null) { continue; }
            obj.SetActive(true);
        }
    }
}
