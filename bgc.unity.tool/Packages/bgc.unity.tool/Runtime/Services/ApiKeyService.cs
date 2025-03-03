using System;
using UnityEngine;
using bgc.unity.tool.ScriptableObjects;

namespace bgc.unity.tool.Services
{
    public class ApiKeyService
    {
        private static string apiKey = "";

        public static string ApiKey => apiKey;

        // APIキーをScriptableObjectから読み込む
        public static void LoadApiKey()
        {
            try
            {
                // ScriptableObjectから設定を読み込む
                TiktokSettings settings = TiktokSettings.Instance;
                
                if (settings != null && !string.IsNullOrEmpty(settings.ApiKey))
                {
                    apiKey = settings.ApiKey;
                    Debug.Log("ScriptableObjectからAPIキーを読み込みました。");
                }
                else
                {
                    Debug.LogWarning("APIキーが設定されていません。TikTok設定ファイルを確認してください。");
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