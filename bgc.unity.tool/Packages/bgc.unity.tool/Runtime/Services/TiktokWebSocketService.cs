using System;
using UnityEngine;
using WebSocketSharp;
using bgc.unity.tool.Models;

namespace bgc.unity.tool.Services
{
    public class TiktokWebSocketService
    {
        // 固定の WebSocket 接続先のベース URL
        private static readonly string baseUrl = "wss://tiktok-live-server-2.onrender.com/ws/";
        
        // ギフトメッセージ受信時に発火するイベント
        public static event Action<GiftMessage> OnGiftReceived;
        
        private static WebSocket ws;
        private static bool isConnected = false;
        
        // 外部から設定可能な username（初期値 "default" なら接続しない）
        public static string Username { get; private set; } = "default";
        
        // 外部から username を設定するための関数
        public static void SetUsername(string username)
        {
            Username = username;
            Debug.Log("Username が設定されました: " + Username);
        }
        
        public static void Connect()
        {
            // username が "default" の場合は接続しない
            if (Username == "default")
            {
                Debug.LogWarning("Usernameが'default'のため、接続しません。");
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
                    // Debug.Log("WebSocketメッセージを受信: " + e.Data);
                    HandleWebSocketMessage(e.Data);
                };

                ws.OnError += (sender, e) => {
                    Debug.LogError("WebSocketエラー: " + e.Message);
                };

                ws.OnClose += (sender, e) => {
                    isConnected = false;
                    Debug.Log("WebSocketサーバーから切断されました。理由: " + e.Reason);
                };

                ws.Connect();
            }
            catch (Exception e)
            {
                Debug.LogError("WebSocketサーバーへの接続に失敗しました: " + e.Message);
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
                // JSON を GiftMessage 型に変換
                GiftMessage giftMsg = JsonUtility.FromJson<GiftMessage>(message);
                if (giftMsg != null && giftMsg.type == "gift")
                {
                    Debug.Log("ギフトメッセージを受信: " + giftMsg.giftName);
                    OnGiftReceived?.Invoke(giftMsg);
                }
                else
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