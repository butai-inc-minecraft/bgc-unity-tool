using System;
using UnityEngine;
using WebSocketSharp;
using bgc.unity.tool.Models;
using bgc.unity.tool.ScriptableObjects;

namespace bgc.unity.tool.Services
{
    public class TiktokWebSocketService
    {
        // 固定の WebSocket 接続先のベース URL
        private static readonly string baseUrl = "wss://tiktok-live-server-2.onrender.com/ws/";
        
        // ギフトメッセージ受信時に発火するイベント
        public static event Action<GiftMessage> OnGiftReceived;
        
        // 接続エラー発生時に発火するイベント
        public static event Action<string> OnConnectionError;
        
        private static WebSocket ws;
        private static bool isConnected = false;
        
        // 外部から設定可能な username（初期値はScriptableObjectから取得）
        private static string username = "default";
        public static string Username 
        { 
            get => username;
            private set => username = value;
        }
        
        // 初期化処理
        static TiktokWebSocketService()
        {
            // ScriptableObjectから設定を読み込む
            TiktokSettings settings = TiktokSettings.Instance;
            if (settings != null)
            {
                Username = settings.DefaultUsername;
                if (settings.VerboseLogging)
                {
                    Debug.Log("TiktokWebSocketService: 詳細ログが有効です");
                }
            }
        }
        
        // 外部から username を設定するための関数
        public static void SetUsername(string newUsername)
        {
            Username = newUsername;
            Debug.Log("Username が設定されました: " + Username);
        }
        
        // 接続状態を取得するプロパティ
        public static bool IsConnected => isConnected;
        
        public static void Connect()
        {
            // APIキーが設定されているか確認
            string apiKey = ApiKeyService.ApiKey;
            if (string.IsNullOrEmpty(apiKey) || apiKey == "xxxxxxxxxxx")
            {
                string errorMessage = "APIキーが設定されていないため、接続できません。TikTok設定ファイルでAPIキーを設定してください。";
                Debug.LogError(errorMessage);
                OnConnectionError?.Invoke(errorMessage);
                return;
            }
            
            // username が "default" の場合は接続しない
            if (Username == "default")
            {
                string errorMessage = "Usernameが'default'のため、接続しません。有効なユーザー名を設定してください。";
                Debug.LogWarning(errorMessage);
                OnConnectionError?.Invoke(errorMessage);
                return;
            }

            // 接続先URLを username を付加して生成
            string fullUrl = baseUrl + Username;

            if (isConnected)
            {
                Debug.LogWarning("既に接続済みです。");
                return;
            }

            Debug.Log("接続先URL: " + fullUrl);

            try
            {
                ws = new WebSocket(fullUrl);

                ws.OnOpen += (sender, e) => {
                    isConnected = true;
                    Debug.Log("WebSocketサーバーに接続しました: " + fullUrl);
                    SendApiKeyAndUsername();
                };

                ws.OnMessage += (sender, e) => {
                    // 詳細ログが有効な場合のみ出力
                    if (TiktokSettings.Instance.VerboseLogging)
                    {
                        Debug.Log("WebSocketメッセージを受信: " + e.Data);
                    }
                    HandleWebSocketMessage(e.Data);
                };

                ws.OnError += (sender, e) => {
                    string errorMessage = "WebSocketエラー: " + e.Message;
                    Debug.LogError(errorMessage);
                    OnConnectionError?.Invoke(errorMessage);
                };

                ws.OnClose += (sender, e) => {
                    isConnected = false;
                    Debug.Log("WebSocketサーバーから切断されました。理由: " + e.Reason);
                    if (!string.IsNullOrEmpty(e.Reason))
                    {
                        OnConnectionError?.Invoke("切断理由: " + e.Reason);
                    }
                };

                ws.Connect();
            }
            catch (Exception e)
            {
                string errorMessage = "WebSocketサーバーへの接続に失敗しました: " + e.Message;
                Debug.LogError(errorMessage);
                OnConnectionError?.Invoke(errorMessage);
            }
        }
        
        // API キーと Username を一緒に送信する関数
        private static void SendApiKeyAndUsername()
        {
            // ApiKeyServiceからAPIキーを取得
            string apiKey = ApiKeyService.ApiKey;
            
            // JSON形式で送信
            string message = $"{{\"apiKey\": \"{apiKey}\", \"username\": \"{Username}\"}}";
            ws.Send(message);
            Debug.Log("API キーと Username を送信しました。");
        }
        
        // 受信したメッセージを解析して、ギフトメッセージの場合はイベントを発火する
        private static void HandleWebSocketMessage(string message)
        {
            try
            {
                // エラーメッセージの確認
                if (message.Contains("error") || message.Contains("Error") || message.Contains("ERROR"))
                {
                    Debug.LogError("サーバーからエラーメッセージを受信: " + message);
                    OnConnectionError?.Invoke("サーバーエラー: " + message);
                    return;
                }
                
                // JSON を GiftMessage 型に変換
                GiftMessage giftMsg = JsonUtility.FromJson<GiftMessage>(message);
                if (giftMsg != null && giftMsg.type == "gift")
                {
                    Debug.Log("ギフトメッセージを受信: " + giftMsg.giftName);
                    OnGiftReceived?.Invoke(giftMsg);
                }
                else if (TiktokSettings.Instance.VerboseLogging)
                {
                    Debug.Log("他のメッセージを受信: " + message);
                }
                // 他の messageType に応じた処理もここで追加可能
            }
            catch (Exception ex)
            {
                Debug.LogWarning("WebSocketメッセージの解析に失敗: " + ex.Message);
            }
        }
        
        public static void Disconnect()
        {
            if (ws != null && isConnected)
            {
                ws.Close();
                isConnected = false;
            }
            else
            {
                Debug.LogWarning("接続がないか、既に切断されています。");
            }
        }
        
        public static void Cleanup()
        {
            if (ws != null)
            {
                if (isConnected)
                {
                    ws.Close();
                    isConnected = false;
                }
                ws = null;
            }
        }
    }
} 