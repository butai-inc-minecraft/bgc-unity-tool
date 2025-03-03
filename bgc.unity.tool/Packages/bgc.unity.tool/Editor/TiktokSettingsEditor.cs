using UnityEngine;
using UnityEditor;
using bgc.unity.tool.ScriptableObjects;
using System.IO;

namespace bgc.unity.tool.Editor
{
    [CustomEditor(typeof(TiktokSettings))]
    public class TiktokSettingsEditor : UnityEditor.Editor
    {
        private SerializedProperty apiKeyProperty;
        private SerializedProperty defaultUsernameProperty;
        private SerializedProperty verboseLoggingProperty;
        
        private bool showSecurityWarning = false;

        private void OnEnable()
        {
            apiKeyProperty = serializedObject.FindProperty("apiKey");
            defaultUsernameProperty = serializedObject.FindProperty("defaultUsername");
            verboseLoggingProperty = serializedObject.FindProperty("verboseLogging");
            
            // APIキーが設定されているか確認
            CheckApiKeyStatus();
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("TikTok API設定", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            // APIキー入力フィールド
            EditorGUILayout.PropertyField(apiKeyProperty, new GUIContent("APIキー", "TikTok APIキーを入力してください"));
            
            // セキュリティ警告の表示
            if (showSecurityWarning)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(
                    "APIキーが設定されています。このアセットをGitなどのバージョン管理システムにコミットする前に、APIキーを削除してください。" +
                    "\n\n機密情報を公開リポジトリにアップロードすると、セキュリティリスクが発生します。", 
                    MessageType.Warning);
                
                if (GUILayout.Button("APIキーをクリア"))
                {
                    apiKeyProperty.stringValue = "";
                    serializedObject.ApplyModifiedProperties();
                    CheckApiKeyStatus();
                }
                EditorGUILayout.Space();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("接続設定", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(defaultUsernameProperty, new GUIContent("デフォルトユーザー名", "接続時のデフォルトユーザー名"));

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("デバッグ設定", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(verboseLoggingProperty, new GUIContent("詳細ログ", "詳細なログを出力するかどうか"));

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox(
                "このファイルは Resources/TiktokSettings.asset として保存してください。" +
                "\n\nAPIキーなどの機密情報を含むため、.gitignoreに追加することをお勧めします。", 
                MessageType.Info);

            // 変更を適用
            if (serializedObject.hasModifiedProperties)
            {
                serializedObject.ApplyModifiedProperties();
                CheckApiKeyStatus();
            }
        }

        private void CheckApiKeyStatus()
        {
            // APIキーが設定されているか確認
            showSecurityWarning = !string.IsNullOrEmpty(apiKeyProperty.stringValue);
            
            // Gitリポジトリ内にあるか確認
            if (showSecurityWarning && IsInGitRepository())
            {
                // .gitignoreに追加されているか確認
                if (!IsInGitignore())
                {
                    Debug.LogWarning("TiktokSettings.assetが.gitignoreに追加されていません。機密情報が公開される可能性があります。");
                }
            }
        }

        private bool IsInGitRepository()
        {
            // プロジェクトのルートディレクトリを取得
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            
            // .gitディレクトリが存在するか確認
            return Directory.Exists(Path.Combine(projectRoot, ".git"));
        }

        private bool IsInGitignore()
        {
            // プロジェクトのルートディレクトリを取得
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string gitignorePath = Path.Combine(projectRoot, ".gitignore");
            
            // .gitignoreファイルが存在するか確認
            if (!File.Exists(gitignorePath))
            {
                return false;
            }
            
            // .gitignoreファイルの内容を読み込む
            string[] lines = File.ReadAllLines(gitignorePath);
            
            // TiktokSettings.assetが含まれているか確認
            foreach (string line in lines)
            {
                if (line.Contains("TiktokSettings.asset") || line.Contains("Resources/TiktokSettings"))
                {
                    return true;
                }
            }
            
            return false;
        }

        [MenuItem("BGC/TikTok/設定ファイルを作成")]
        public static void CreateTiktokSettings()
        {
            // Resourcesディレクトリが存在するか確認
            string resourcesPath = Path.Combine(Application.dataPath, "Resources");
            if (!Directory.Exists(resourcesPath))
            {
                Directory.CreateDirectory(resourcesPath);
                AssetDatabase.Refresh();
            }
            
            // 設定ファイルのパス
            string assetPath = "Assets/Resources/TiktokSettings.asset";
            
            // 既存の設定ファイルを確認
            TiktokSettings existingSettings = AssetDatabase.LoadAssetAtPath<TiktokSettings>(assetPath);
            if (existingSettings != null)
            {
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = existingSettings;
                Debug.Log("既存の設定ファイルを開きました: " + assetPath);
                return;
            }
            
            // 新しい設定ファイルを作成
            TiktokSettings settings = CreateInstance<TiktokSettings>();
            AssetDatabase.CreateAsset(settings, assetPath);
            AssetDatabase.SaveAssets();
            
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = settings;
            Debug.Log("新しい設定ファイルを作成しました: " + assetPath);
            
            // サンプル設定ファイルも作成
            CreateSampleSettings();
            
            // .gitignoreに追加するか確認
            if (EditorUtility.DisplayDialog("Gitignoreに追加", 
                "TiktokSettings.assetを.gitignoreに追加しますか？\n\n" +
                "APIキーなどの機密情報を含むため、バージョン管理から除外することをお勧めします。", 
                "はい", "いいえ"))
            {
                AddToGitignore();
            }
        }
        
        [MenuItem("BGC/TikTok/サンプル設定ファイルを作成")]
        public static void CreateSampleSettings()
        {
            // Resourcesディレクトリが存在するか確認
            string resourcesPath = Path.Combine(Application.dataPath, "Resources");
            if (!Directory.Exists(resourcesPath))
            {
                Directory.CreateDirectory(resourcesPath);
                AssetDatabase.Refresh();
            }
            
            // サンプル設定ファイルのパス
            string samplePath = "Assets/Resources/TiktokSettings.sample.asset";
            
            // 既存のサンプル設定ファイルを確認
            TiktokSettings existingSample = AssetDatabase.LoadAssetAtPath<TiktokSettings>(samplePath);
            if (existingSample != null)
            {
                EditorUtility.FocusProjectWindow();
                Selection.activeObject = existingSample;
                Debug.Log("既存のサンプル設定ファイルを開きました: " + samplePath);
                return;
            }
            
            // 新しいサンプル設定ファイルを作成
            TiktokSettings sampleSettings = CreateInstance<TiktokSettings>();
            // サンプル値を設定（APIキーは空のまま）
            // SerializedObjectを使って値を設定
            SerializedObject serializedObject = new SerializedObject(sampleSettings);
            SerializedProperty defaultUsernameProp = serializedObject.FindProperty("defaultUsername");
            SerializedProperty verboseLoggingProp = serializedObject.FindProperty("verboseLogging");
            
            defaultUsernameProp.stringValue = "sample_username";
            verboseLoggingProp.boolValue = true;
            
            serializedObject.ApplyModifiedProperties();
            
            AssetDatabase.CreateAsset(sampleSettings, samplePath);
            AssetDatabase.SaveAssets();
            
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = sampleSettings;
            Debug.Log("サンプル設定ファイルを作成しました: " + samplePath);
            
            // README.txtファイルも作成して使い方を説明
            string readmePath = Path.Combine(resourcesPath, "TiktokSettings_README.txt");
            string readmeContent = 
                "TikTok設定ファイルの使い方\n" +
                "====================\n\n" +
                "1. TiktokSettings.asset - 実際の設定ファイル（.gitignoreに追加してください）\n" +
                "   - APIキーなどの機密情報を含むため、Gitにコミットしないでください\n\n" +
                "2. TiktokSettings.sample.asset - サンプル設定ファイル\n" +
                "   - 設定ファイルの構造を示すためのサンプルです\n" +
                "   - APIキーは含まれていません\n" +
                "   - このファイルはGitにコミットして構いません\n\n" +
                "新しい環境でセットアップする場合：\n" +
                "1. TiktokSettings.sample.assetをコピーしてTiktokSettings.assetを作成\n" +
                "2. TiktokSettings.assetにAPIキーを設定\n" +
                "3. アプリケーションを実行\n";
            
            File.WriteAllText(readmePath, readmeContent);
            AssetDatabase.Refresh();
            
            Debug.Log("README.txtファイルを作成しました: " + readmePath);
        }
        
        private static void AddToGitignore()
        {
            // プロジェクトのルートディレクトリを取得
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string gitignorePath = Path.Combine(projectRoot, ".gitignore");
            
            // .gitignoreファイルが存在するか確認
            if (!File.Exists(gitignorePath))
            {
                // .gitignoreファイルを作成
                File.WriteAllText(gitignorePath, "# TikTok Settings\nAssets/Resources/TiktokSettings.asset\nAssets/Resources/TiktokSettings.asset.meta\n");
                Debug.Log(".gitignoreファイルを作成し、TiktokSettings.assetとmetaファイルを追加しました。");
                return;
            }
            
            // .gitignoreファイルの内容を読み込む
            string[] lines = File.ReadAllLines(gitignorePath);
            
            // TiktokSettings.assetが含まれているか確認
            bool hasAsset = false;
            bool hasMeta = false;
            
            foreach (string line in lines)
            {
                if (line.Contains("TiktokSettings.asset") && !line.Contains(".meta"))
                {
                    hasAsset = true;
                }
                if (line.Contains("TiktokSettings.asset.meta"))
                {
                    hasMeta = true;
                }
            }
            
            // .gitignoreファイルに追加
            using (StreamWriter writer = File.AppendText(gitignorePath))
            {
                if (!hasAsset && !hasMeta)
                {
                    writer.WriteLine();
                    writer.WriteLine("# TikTok Settings");
                }
                
                if (!hasAsset)
                {
                    writer.WriteLine("Assets/Resources/TiktokSettings.asset");
                }
                
                if (!hasMeta)
                {
                    writer.WriteLine("Assets/Resources/TiktokSettings.asset.meta");
                }
            }
            
            if (!hasAsset || !hasMeta)
            {
                Debug.Log("TiktokSettings.assetとmetaファイルを.gitignoreに追加しました。");
            }
            else
            {
                Debug.Log("TiktokSettings.assetとmetaファイルは既に.gitignoreに追加されています。");
            }
        }
    }
} 