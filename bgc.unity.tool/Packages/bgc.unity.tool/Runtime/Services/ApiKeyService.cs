using System;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using bgc.unity.tool.Models;

namespace bgc.unity.tool.Services
{
    public class ApiKeyService
    {
        private static string apiKey = "";

        public static string ApiKey => apiKey;

        // APIキーをJSONファイルから読み込む
        public static void LoadApiKey()
        {
            try
            {
                // Runtimeディレクトリ内のapiKey.jsonファイルのパスを取得
                string apiKeyPath = "";

#if UNITY_EDITOR
                // エディタ実行時はパッケージ内のファイルを直接読み込む
                string[] guids = AssetDatabase.FindAssets("apiKey", new[] { "Packages/bgc.unity.tool/Runtime" });
                if (guids.Length > 0)
                {
                    apiKeyPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                    Debug.Log("APIキーファイルのパス: " + apiKeyPath);
                }
#else
                // ビルド時はStreamingAssetsを使用
                apiKeyPath = Path.Combine(Application.streamingAssetsPath, "apiKey.json");
#endif

                // ファイルが存在するか確認
                if (!string.IsNullOrEmpty(apiKeyPath) && File.Exists(apiKeyPath))
                {
                    // ファイルを読み込む
                    string jsonText = "";
                    
#if UNITY_EDITOR
                    // エディタ実行時
                    jsonText = File.ReadAllText(apiKeyPath);
#else
                    // ビルド時
                    jsonText = File.ReadAllText(apiKeyPath);
#endif

                    // JSONをデシリアライズ
                    ApiKeyConfig config = JsonUtility.FromJson<ApiKeyConfig>(jsonText);
                    
                    if (config != null && !string.IsNullOrEmpty(config.apiKey))
                    {
                        apiKey = config.apiKey;
                        Debug.Log("APIキーを読み込みました。");
                    }
                    else
                    {
                        Debug.LogWarning("APIキーの読み込みに失敗しました。設定ファイルの形式が正しくありません。");
                        apiKey = "xxxxxxxxxxx"; // デフォルト値を使用
                    }
                }
                else
                {
                    Debug.LogWarning("APIキー設定ファイルが見つかりません: " + apiKeyPath);
                    apiKey = "xxxxxxxxxxx"; // デフォルト値を使用
                }
            }
            catch (Exception e)
            {
                Debug.LogError("APIキーの読み込み中にエラーが発生しました: " + e.Message);
                apiKey = "xxxxxxxxxxx"; // デフォルト値を使用
            }
        }
    }
} 