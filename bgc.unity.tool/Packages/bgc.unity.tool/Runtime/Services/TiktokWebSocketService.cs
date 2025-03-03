using System;
using System.Collections.Generic;
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
        
        // 部屋の視聴者情報受信時に発火するイベント
        public static event Action<RoomUserMessage> OnRoomUserReceived;
        
        // いいねメッセージ受信時に発火するイベント
        public static event Action<LikeMessage> OnLikeReceived;
        
        // チャットメッセージ受信時に発火するイベント
        public static event Action<ChatMessage> OnChatReceived;
        
        // 接続エラー発生時に発火するイベント
        public static event Action<string> OnConnectionError;
        
        private static WebSocket ws;
        private static bool isConnected = false;
        
        // メインスレッドで処理するためのメッセージキュー
        private static readonly Queue<string> messageQueue = new Queue<string>();
        private static bool verboseLogging = false;
        
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
            
            // Unity更新時に呼ばれるコールバックを登録
            Application.quitting += Cleanup;
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
                // 設定を事前にキャッシュ
                verboseLogging = TiktokSettings.Instance.VerboseLogging;
                
                ws = new WebSocket(fullUrl);

                ws.OnOpen += (sender, e) => {
                    isConnected = true;
                    Debug.Log("WebSocketサーバーに接続しました: " + fullUrl);
                    SendApiKeyAndUsername();
                };

                ws.OnMessage += (sender, e) => {
                    // メッセージをキューに追加するだけ（メインスレッドでの処理は別途行う）
                    lock (messageQueue)
                    {
                        messageQueue.Enqueue(e.Data);
                    }
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
        
        // メインスレッドで呼び出される必要があるメソッド
        // MonoBehaviourのUpdateなどから呼び出す必要がある
        public static void ProcessMessageQueue()
        {
            if (messageQueue.Count == 0) return;
            
            // キューからメッセージを取り出して処理
            lock (messageQueue)
            {
                while (messageQueue.Count > 0)
                {
                    string message = messageQueue.Dequeue();
                    
                    // 詳細ログが有効な場合のみ出力（事前にキャッシュした値を使用）
                    if (verboseLogging)
                    {
                        Debug.Log("WebSocketメッセージを処理: " + message);
                    }
                    
                    HandleWebSocketMessage(message);
                }
            }
        }
        
        // 受信したメッセージを解析して、メッセージタイプに応じたイベントを発火する
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
                
                // メッセージタイプの確認
                if (message.Contains("\"type\":\"gift\"") || message.Contains("\"type\": \"gift\""))
                {
                    // ギフトメッセージの処理
                    GiftMessage giftMsg = JsonUtility.FromJson<GiftMessage>(message);
                    if (giftMsg != null)
                    {
                        // Debug.Log("ギフトメッセージを受信: " + giftMsg.giftName);
                        OnGiftReceived?.Invoke(giftMsg);
                    }
                }
                else if (message.Contains("\"type\":\"roomUser\"") || message.Contains("\"type\": \"roomUser\""))
                {
                    // 部屋の視聴者情報メッセージの処理
                    RoomUserMessage roomUserMsg = JsonUtility.FromJson<RoomUserMessage>(message);
                    if (roomUserMsg != null)
                    {
                        // Debug.Log($"部屋の視聴者情報を受信: 視聴者数 {roomUserMsg.viewerCount}人");
                        OnRoomUserReceived?.Invoke(roomUserMsg);
                    }
                }
                else if (message.Contains("\"type\":\"like\"") || message.Contains("\"type\": \"like\""))
                {
                    // いいねメッセージの処理
                    LikeMessage likeMsg = JsonUtility.FromJson<LikeMessage>(message);
                    if (likeMsg != null)
                    {
                        // Debug.Log($"いいねメッセージを受信: {likeMsg.nickname}さんが{likeMsg.likeCount}いいねしました (合計: {likeMsg.totalLikeCount})");
                        OnLikeReceived?.Invoke(likeMsg);
                    }
                }
                else if (message.Contains("\"type\":\"chat\"") || message.Contains("\"type\": \"chat\""))
                {
                    // チャットメッセージの処理
                    ChatMessage chatMsg = JsonUtility.FromJson<ChatMessage>(message);
                    if (chatMsg != null)
                    {
                        // Debug.Log($"チャットメッセージを受信: {chatMsg.nickname}さん「{chatMsg.comment}」");
                        OnChatReceived?.Invoke(chatMsg);
                    }
                }
                else if (verboseLogging) // キャッシュした値を使用
                {
                    Debug.Log("他のメッセージを受信: " + message);
                }
                // 他の messageType に応じた処理もここで追加可能
            }
            catch (Exception ex)
            {
                Debug.LogError("メッセージの処理中にエラーが発生しました: " + ex.Message);
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