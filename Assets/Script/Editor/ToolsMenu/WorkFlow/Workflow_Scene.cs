using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

public class Workflow_Scene
{
    private const string Root = ToolsMenuConstants.Root + "Workflow/Scene/";
    private const int PriorityBase = 130;

    [MenuItem(Root + "Boot", false, PriorityBase + 0)]
    private static void SceneBoot()
    {
        JumpScene("Boot");
    }

    [MenuItem(Root + "DevMenu", false, PriorityBase + 1)]
    private static void SceneDevMenue()
    {
        JumpScene("DevMenu");
    }

    [MenuItem(Root + "RootUI", false, PriorityBase + 2)]
    private static void SceneRootUI()
    {
          JumpScene("RootUI");
    }

    [MenuItem(Root + "Title", false, PriorityBase + 3)]
    private static void SceneTitle()
    {
          JumpScene("Title");
    }

    [MenuItem(Root + "InGame", false, PriorityBase + 4)]
    private static void SceneInGame()
    {
          JumpScene("InGame");
    }

    [MenuItem(Root + "Result", false, PriorityBase + 5)]
    private static void SceneResult()
    {
          JumpScene("Result");
    }

    private const string AssetPath = "Assets/ScriptableObject/Editor/SceneBookmarks.asset";

    private static void JumpScene(string key)
    {
        var bookmarks = AssetDatabase.LoadAssetAtPath<SceneBookmarks>(AssetPath);
        if (bookmarks == null)
        {
            //Debug.LogError($"SceneBookmarks not found: {AssetPath}");
            return;
        }

        var path = bookmarks.TryGetPath(key);
        if (string.IsNullOrEmpty(path))
        {
            //Debug.LogError("Scene not found for key: Boot");
            return;
        }

        // 変更があれば保存確認
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
    }
}


