using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Windows.Speech;

public class VoiceDestroyObject : MonoBehaviour
{
    [Header("消したいオブジェクト")]
    [SerializeField] private GameObject targetObject;

    [Header("反応する単語")]
    [SerializeField] private string destroyWord = "消えろ";

    [Header("消す代わりに非表示にする")]
    [SerializeField] private bool onlyHide = false;

    private KeywordRecognizer keywordRecognizer;
    private Dictionary<string, System.Action> keywordActions = new Dictionary<string, System.Action>();

    void Start()
    {
        if (targetObject == null)
        {
            Debug.LogError("Target Object が設定されていません");
            return;
        }

        if (string.IsNullOrEmpty(destroyWord))
        {
            Debug.LogError("Destroy Word が空です");
            return;
        }

        keywordActions.Add(destroyWord, DeleteTarget);

        keywordRecognizer = new KeywordRecognizer(keywordActions.Keys.ToArray());
        keywordRecognizer.OnPhraseRecognized += OnPhraseRecognized;
        keywordRecognizer.Start();

        Debug.Log("音声認識開始: " + destroyWord);
    }

    private void OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        Debug.Log("認識した言葉: " + args.text);

        if (keywordActions.ContainsKey(args.text))
        {
            keywordActions[args.text].Invoke();
        }
    }

    private void DeleteTarget()
    {
        if (targetObject == null) return;

        if (onlyHide)
        {
            targetObject.SetActive(false);
            Debug.Log("オブジェクトを非表示にしました");
        }
        else
        {
            Destroy(targetObject);
            Debug.Log("オブジェクトを削除しました");
        }
    }

    private void OnDestroy()
    {
        if (keywordRecognizer != null)
        {
            if (keywordRecognizer.IsRunning)
            {
                keywordRecognizer.Stop();
            }

            keywordRecognizer.OnPhraseRecognized -= OnPhraseRecognized;
            keywordRecognizer.Dispose();
        }
    }
}