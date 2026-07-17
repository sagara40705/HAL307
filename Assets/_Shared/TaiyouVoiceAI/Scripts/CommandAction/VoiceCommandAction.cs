using System.Collections;
using UnityEngine;

public class VoiceCommandAction : MonoBehaviour
{
    [Header("Menu")]
    //[SerializeField] private VoiceMenuController menuController;

    [Header("Legacy Objects")]
    [SerializeField] private GameObject powerObject;
    [SerializeField] private GameObject yaaObject;
    [SerializeField] private GameObject muscleObject;

    [Header("Voice Gate")]
    [SerializeField] private float minimumDb = -20f;
    [SerializeField] private bool onlyHide = true;
    [SerializeField] private float hideTime = 5f;
    [SerializeField] private bool logCommand = true;

    private void Awake()
    {

    }

    public void ExecuteCommand(string command, float volume)
    {
        if (string.IsNullOrWhiteSpace(command) || command == "none")
        {
            return;
        }

        float db = VolumeToDb(volume);
        if (db < minimumDb)
        {
            if (logCommand)
            {
                Debug.Log($"[Voice] ignored small input command={command} db={db:F1}");
            }
            return;
        }

        if (logCommand)
        {
            Debug.Log($"[Voice] command={command} volume={volume:F4} db={db:F1}");
        }

        if (IsMenuCommand(command))
        {
            //menuController.ExecuteVoiceCommand(command);
            return;
        }

        switch (command)
        {
            case "power":
                HideOrDestroy(powerObject, "power");
                break;

            case "yaa":
                HideOrDestroy(yaaObject, "yaa");
                break;

            case "muscle":
                HideOrDestroy(muscleObject, "muscle");
                break;
        }
    }

    private static bool IsMenuCommand(string command)
    {
        return command == "ue" ||
               command == "sita" ||
               command == "kettei" ||
               command == "time";
    }

    private void HideOrDestroy(GameObject obj, string label)
    {
        if (obj == null)
        {
            Debug.LogWarning(label + " target is not assigned.");
            return;
        }

        if (onlyHide)
        {
            if (!obj.activeSelf)
            {
                return;
            }

            obj.SetActive(false);
            StartCoroutine(ShowAfterDelay(obj));

            if (logCommand)
            {
                Debug.Log(label + " hidden.");
            }
        }
        else
        {
            Destroy(obj);

            if (logCommand)
            {
                Debug.Log(label + " destroyed.");
            }
        }
    }

    private IEnumerator ShowAfterDelay(GameObject obj)
    {
        yield return new WaitForSeconds(hideTime);

        if (obj != null)
        {
            obj.SetActive(true);

            if (logCommand)
            {
                Debug.Log(obj.name + " shown.");
            }
        }
    }

    private float VolumeToDb(float volume)
    {
        return 20f * Mathf.Log10(Mathf.Max(volume, 0.000001f));
    }
}
