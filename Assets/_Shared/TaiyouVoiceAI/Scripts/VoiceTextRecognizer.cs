using UnityEngine;
using UnityEngine.Windows.Speech;

public class VoiceTextRecognizer : MonoBehaviour
{
    private DictationRecognizer dictationRecognizer;

    [SerializeField]
    private VoiceCommandAction voiceCommandAction;

    private void Start()
    {
        dictationRecognizer = new DictationRecognizer();

        dictationRecognizer.DictationResult += OnDictationResult;

        dictationRecognizer.Start();

        Debug.Log("音声認識開始");
    }

    private void OnDictationResult(string text, ConfidenceLevel confidence)
    {
        Debug.Log("認識文字: " + text);

        string command = NarrowCommandFromText(text);

        if (command != "none")
        {
            Debug.Log("確定コマンド: " + command);

            voiceCommandAction.ExecuteCommand(command, 1.0f);
        }
    }

    private string NarrowCommandFromText(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "none";

        // マッスル系
        if (text.StartsWith("マッ") ||
            text.StartsWith("まっ"))
        {
            return "muscle";
        }

        // パワー系
        if (text.StartsWith("パ") ||
            text.StartsWith("ぱ"))
        {
            return "power";
        }

        // ヤー系
        if (text.StartsWith("ヤ") ||
            text.StartsWith("や"))
        {
            return "yaa";
        }

        return "none";
    }

    private void OnDestroy()
    {
        if (dictationRecognizer != null)
        {
            dictationRecognizer.Stop();
            dictationRecognizer.Dispose();
        }
    }
}
