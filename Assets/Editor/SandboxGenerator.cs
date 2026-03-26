using System.IO;
using UnityEditor;
using UnityEngine;

public static class SandboxGenerator
{
    private const string RootFolder = "Assets/_Sandbox";

    private const int Priority = 100;

    [InitializeOnLoadMethod]
    private static void RegisterDelayedMenu()
    {
        EditorApplication.delayCall -= ShowInputOnNextRequest;
    }

    private static bool _pendingInput;
    private static void ShowInputOnNextRequest()
    {
        if (!_pendingInput)
        {
            return;
        }

        _pendingInput = false;
        string personName = SandboxNameInputWindow.ShowWindowAndGetResult();
        if (string.IsNullOrWhiteSpace(personName))
        {
            return;
        }

        CreateSandboxInternal(personName);
    }

    [MenuItem("Tools/Sandbox/Create Sandbox", false, Priority)]
    public static void CreateSandboxPrompt()
    {
        _pendingInput = true;
        EditorApplication.delayCall += ShowInputOnNextRequest;
    }

    private static void CreateSandboxInternal(string rawName)
    {
        string personName = SanitizeName(rawName);
        if (string.IsNullOrWhiteSpace(personName))
        {
            EditorUtility.DisplayDialog("Error", "有効な名前を入力してください。", "OK");
            return;
        }
        if (!System.Text.RegularExpressions.Regex.IsMatch(personName, @"^[A-Za-z0-9]+$"))
        {
            EditorUtility.DisplayDialog("Error", "英数字のみ使用してください。", "OK");
            return;
        }

        string basePath = $"{RootFolder}/{personName}";
        if (AssetDatabase.IsValidFolder(basePath))
        {
            EditorUtility.DisplayDialog(
                "Error",
                $"Sandbox '{personName}' は既に存在します。",
                "OK"
            );
            return;
        }

        EnsureFolder("Assets", "_Sandbox");
        EnsureFolder(RootFolder, personName);

        CreateSubFolder(basePath, "Scenes");
        CreateSubFolder(basePath, "Scripts");
        CreateSubFolder(basePath, "Prefabs");
        CreateSubFolder(basePath, "Materials");
        CreateSubFolder(basePath, "Models");
        CreateSubFolder(basePath, "Audio");
        CreateSubFolder(basePath, "UI");
        CreateSubFolder(basePath, "Editor");

        CreateAsmdef(basePath, personName);
        CreateReadme(basePath, personName);

        AssetDatabase.Refresh();
        AssetDatabase.SaveAssets();

        Object folderObj = AssetDatabase.LoadAssetAtPath<Object>(basePath);
        EditorGUIUtility.PingObject(folderObj);
        Selection.activeObject = folderObj;

        EditorUtility.DisplayDialog(
            "Sandbox Created",
            $"Sandbox '{personName}' を作成しました。",
            "OK"
        );
    }

    private static void CreateSubFolder(string parent, string folderName)
    {
        if (!AssetDatabase.IsValidFolder($"{parent}/{folderName}"))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    private static void EnsureFolder(string parent, string folderName)
    {
        if (!AssetDatabase.IsValidFolder($"{parent}/{folderName}"))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }

    private static string FindAsmdefGuidByName(string asmdefName)
    {
        string[] guids = AssetDatabase.FindAssets($"{asmdefName} t:asmdef");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);

            if (fileName == asmdefName)
            {
                return guid;
            }
        }

        return null;
    }

    private static void CreateAsmdef(string basePath, string personName)
    {
        string asmdefPath = $"{basePath}/{personName}.Sandbox.asmdef";
        if (File.Exists(asmdefPath))
        {
            return;
        }

        string gameAsmdefGuid = FindAsmdefGuidByName("Game");
        if (string.IsNullOrEmpty(gameAsmdefGuid))
        {
            EditorUtility.DisplayDialog(
                "Error",
                "Game.asmdef が見つかりませんでした。相良に連絡ください",
                "OK"
            );
            return;
        }

        string json =
$@"{{
    ""name"": ""{personName}.Sandbox"",
    ""rootNamespace"": ""{personName}.Sandbox"",
    ""references"": [
        ""GUID:{gameAsmdefGuid}""
    ],
    ""includePlatforms"": [],
    ""excludePlatforms"": [],
    ""allowUnsafeCode"": false,
    ""overrideReferences"": false,
    ""precompiledReferences"": [],
    ""autoReferenced"": false,
    ""defineConstraints"": [],
    ""versionDefines"": [],
    ""noEngineReferences"": false
}}";

        File.WriteAllText(asmdefPath, json);
    }

    private static void CreateReadme(string basePath, string personName)
    {
        string readmePath = $"{basePath}/README.txt";
        if (File.Exists(readmePath))
        {
            return;
        }

        string text =
$@"Sandbox: {personName}

Rules:
- 実験用コード/Prefab/Sceneはこの配下に置く
- 本番(Game)からSandboxを参照しない
- 使うと決まったものは本番フォルダへ移動する
- 壊してもよい / 削除してもよい
- 右のファイルは触らない^^
";

        File.WriteAllText(readmePath, text);
    }

    private static string SanitizeName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        string invalid = new string(Path.GetInvalidFileNameChars());
        foreach (char c in invalid)
        {
            value = value.Replace(c.ToString(), "");
        }

        value = value.Replace(" ", "");
        value = value.Replace(".", "");
        value = value.Replace("/", "");
        value = value.Replace("\\", "");

        return value.Trim();
    }
}

public class SandboxNameInputWindow : EditorWindow
{
    private string _input = "Gabon";
    private bool _submitted;
    private bool _cancelled;

    public static string ShowWindowAndGetResult()
    {
        SandboxNameInputWindow window = CreateInstance<SandboxNameInputWindow>();
        window.titleContent = new GUIContent("Create Sandbox");
        window.position = new Rect(
            Screen.currentResolution.width / 2f - 150f,
            Screen.currentResolution.height / 2f - 60f,
            300f,
            120f
        );
        window.minSize = new Vector2(300f, 120f);
        window.maxSize = new Vector2(300f, 120f);
        window.ShowModalUtility();

        if (window._cancelled)
        {
            return null;
        }

        return window._submitted ? window._input : null;
    }

    private void OnGUI()
    {
        GUILayout.Label("Sandbox名", EditorStyles.boldLabel);
        GUI.SetNextControlName("SandboxNameField");
        _input = EditorGUILayout.TextField(_input);

        GUILayout.Space(10);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Cancel"))
            {
                _cancelled = true;
                Close();
            }

            if (GUILayout.Button("Create"))
            {
                _submitted = true;
                Close();
            }
        }

        EditorGUI.FocusTextInControl("SandboxNameField");
    }


}
