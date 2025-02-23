using UnityEngine;
using UnityEditor;

public class APIKeySettingsWindow : EditorWindow
{
    private APIKeySettings settings;
    private const string assetPath = "Assets/APIKeySettings.asset";

    [MenuItem("自作メニュー/Validate")]
private static void Validate() { }


    [MenuItem("Window/API Key Settings")]
    public static void ShowWindow()
    {
        GetWindow<APIKeySettingsWindow>("API Key Settings");
    }

    private void OnEnable()
    {
        // 既存の設定アセットを読み込むか、新規作成する
        settings = AssetDatabase.LoadAssetAtPath<APIKeySettings>(assetPath);
        if (settings == null)
        {
            settings = CreateInstance<APIKeySettings>();
            AssetDatabase.CreateAsset(settings, assetPath);
            AssetDatabase.SaveAssets();
        }
    }

    private void OnGUI()
    {
        GUILayout.Label("API キーを入力してください", EditorStyles.boldLabel);
        settings.apiKey = EditorGUILayout.TextField("API Key", settings.apiKey);

        if (GUILayout.Button("保存"))
        {
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("API Key が保存されました。");
        }
    }
}