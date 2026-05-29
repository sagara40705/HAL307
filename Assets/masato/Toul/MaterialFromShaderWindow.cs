using UnityEngine;
using UnityEditor;

public class MaterialFromShaderWindow : EditorWindow
{
    // Material を保存する固定フォルダ
    private const string MaterialFolderPath = "Assets/shaderFolder";

    // ユーザーが選ぶ Shader
    private Shader selectedShader;

    // 作成する Material の名前
    private string materialName = "NewMaterial";

    // Material を適用する対象オブジェクト
    private GameObject targetObject;

    // Unity 上部メニューの Tools からこのウィンドウを開けるようにする
    [MenuItem("Tools/Create Material From Shader")]
    private static void OpenWindow()
    {
        GetWindow<MaterialFromShaderWindow>("Material Creator");
    }

    // ウィンドウの見た目を描画する
    private void OnGUI()
    {
        // タイトル表示
        GUILayout.Label("Shader から Material を作成", EditorStyles.boldLabel);

        // Shader を選ぶ欄
        selectedShader = (Shader)EditorGUILayout.ObjectField(
            "Shader",
            selectedShader,
            typeof(Shader),
            false
        );

        // Material 名を入力する欄
        materialName = EditorGUILayout.TextField("Material Name", materialName);

        // Material を適用するオブジェクトを選ぶ欄
        // true にすると Scene 上のオブジェクトも選べる
        targetObject = (GameObject)EditorGUILayout.ObjectField(
            "Target Object",
            targetObject,
            typeof(GameObject),
            true
        );

        // 保存先フォルダを表示
        EditorGUILayout.LabelField("Save Folder", MaterialFolderPath);

        GUILayout.Space(10);

        // Shader と Object の両方が選ばれている時だけボタンを押せる
        GUI.enabled = selectedShader != null && targetObject != null;

        // ボタンを押したら Material を作成する
        if (GUILayout.Button("Create Material"))
        {
            CreateMaterial();
        }

        // 他の GUI が無効のままにならないように戻す
        GUI.enabled = true;
    }

    // Material を作成して対象オブジェクトに適用する
    private void CreateMaterial()
    {
        // Shader が未選択ならエラー
        if (selectedShader == null)
        {
            EditorUtility.DisplayDialog("Error", "Shader を選択してください。", "OK");
            return;
        }

        // 対象オブジェクトが未選択ならエラー
        if (targetObject == null)
        {
            EditorUtility.DisplayDialog("Error", "適用先のオブジェクトを選択してください。", "OK");
            return;
        }

        // オブジェクトから Renderer を取得
        Renderer targetRenderer = targetObject.GetComponent<Renderer>();

        // Renderer がなければ Material を適用できない
        if (targetRenderer == null)
        {
            EditorUtility.DisplayDialog("Error", "選択したオブジェクトに Renderer がありません。", "OK");
            return;
        }

        // 保存先フォルダが存在しなければ作成する
        EnsureMaterialFolderExists();

        // 選択した Shader を使って新しい Material を作る
        Material newMaterial = new Material(selectedShader);

        // 保存先パスを作る
        // 同名ファイルがある場合は Unity が自動で連番を付けてくれる
        string path = AssetDatabase.GenerateUniqueAssetPath($"{MaterialFolderPath}/{materialName}.mat");

        // Material を .mat ファイルとして保存
        AssetDatabase.CreateAsset(newMaterial, path);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Undo できるように記録してから Renderer に Material を適用する
        Undo.RecordObject(targetRenderer, "Apply Created Material");
        targetRenderer.sharedMaterial = newMaterial;
        EditorUtility.SetDirty(targetRenderer);

        // 作成した Material を Project 上で選択状態にする
        EditorUtility.FocusProjectWindow();
        Selection.activeObject = newMaterial;
    }

    // Hierarchy で選択中のオブジェクトが変わったら自動で反映する
    private void OnSelectionChange()
    {
        if (Selection.activeGameObject != null)
        {
            targetObject = Selection.activeGameObject;
            Repaint();
        }
    }

    // 固定保存先フォルダが存在しなければ作成する
    private static void EnsureMaterialFolderExists()
    {
        if (AssetDatabase.IsValidFolder(MaterialFolderPath))
        {
            return;
        }

        AssetDatabase.CreateFolder("Assets", "shaderFolder");
    }
}
