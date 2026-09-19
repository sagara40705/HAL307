using UnityEngine;
using UnityEngine.Windows.Speech;
using System.Diagnostics;

using Debug = UnityEngine.Debug;

public class VoiceTextRecognizer : MonoBehaviour
{
    [SerializeField]
    private bool enableDebugWindowsRecognition = false;

    private DictationRecognizer dictationRecognizer;

    private void OnEnable()
    {
        if (!enableDebugWindowsRecognition)
        {
            return;
        }

        InitializeRecognizer();
    }

    private void InitializeRecognizer()
    {
        if (dictationRecognizer != null)
        {
            return;
        }

        dictationRecognizer = new DictationRecognizer();
        dictationRecognizer.DictationResult += OnDictationResult;
        dictationRecognizer.DictationError += OnDictationError;
        dictationRecognizer.Start();
    }

    private void OnDictationResult(string text, ConfidenceLevel confidence)
    {
        string command = NarrowCommandFromText(text);
        Debug.Log(
            $"[WindowsVoiceDebug] text={text} " +
            $"mappedCommand={command} " +
            $"forwardedToGame=False"
        );
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

    private void OnDisable()
    {
        DisposeRecognizer();
    }

    private void OnDestroy()
    {
        DisposeRecognizer();
    }

    private void DisposeRecognizer()
    {
        if (dictationRecognizer == null)
        {
            return;
        }

        DictationRecognizer recognizer = dictationRecognizer;
        dictationRecognizer = null;

        recognizer.DictationResult -= OnDictationResult;
        recognizer.DictationError -= OnDictationError;

        if (recognizer.Status == SpeechSystemStatus.Running)
        {
            recognizer.Stop();
        }

        recognizer.Dispose();
    }

    // エラーが発生した場合設定を開く
    private void OnDictationError(string error, int hresult)
    {
        Debug.LogError("音声認識エラー: " + error);

#if UNITY_EDITOR
        bool open = UnityEditor.EditorUtility.DisplayDialog(
            "Windows音声認識がOFFです",
            "Windows音声認識がオフです。\n設定画面を開きますか？",
            "開く",
            "キャンセル"
        );

        if (open)
        {
            OpenSpeechSettings();
        }
#else
    OpenSpeechSettings();
#endif
    }

    private void OpenSpeechSettings()
    {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        Process.Start(new ProcessStartInfo()
        {
            FileName = "ms-settings:privacy-speech",
            UseShellExecute = true
        });
#endif
    }
}
