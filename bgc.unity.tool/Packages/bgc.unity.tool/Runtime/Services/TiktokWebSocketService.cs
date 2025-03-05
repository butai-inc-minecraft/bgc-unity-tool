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
        public static event Action<Models.GiftMessage> OnGiftReceived;
        
        // 部屋の視聴者情報受信時に発火するイベント
        public static event Action<Models.RoomUserMessage> OnRoomUserReceived;
        
        // いいねメッセージ受信時に発火するイベント
        public static event Action<Models.LikeMessage> OnLikeReceived;
        
        // チャットメッセージ受信時に発火するイベント
        public static event Action<Models.ChatMessage> OnChatReceived;
        
        // シェアメッセージ受信時に発火するイベント
        public static event Action<Models.ShareMessage> OnShareReceived;
        
        // フォローメッセージ受信時に発火するイベント
        public static event Action<Models.FollowMessage> OnFollowReceived;
        
        // 接続エラー発生時に発火するイベント
        public static event Action<string> OnConnectionError;
        
        private static WebSocket ws;
        private static bool isConnected = false;
        // 接続処理中かどうかを示すフラグ
        private static bool isConnecting = false;
        // 切断処理中かどうかを示すフラグ
        private static bool isDisconnecting = false;
        // 再接続が必要かどうかを示すフラグ
        private static bool reconnectRequested = false;
        
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
        
        // 接続中かどうかを取得するプロパティ
        public static bool IsConnecting => isConnecting;
        
        // 切断中かどうかを取得するプロパティ
        public static bool IsDisconnecting => isDisconnecting;
        
        public static void Connect()
        {
            // 既に接続中または接続処理中の場合は何もしない
            if (isConnected || isConnecting)
            {
                Debug.LogWarning("既に接続済みまたは接続処理中です。");
                return;
            }
            
            // 切断処理中の場合は、再接続フラグを立てて終了
            if (isDisconnecting)
            {
                Debug.Log("切断処理中のため、切断完了後に再接続します。");
                reconnectRequested = true;
                return;
            }
            
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

            Debug.Log("接続先URL: " + fullUrl);

            try
            {
                // 接続処理中フラグを立てる
                isConnecting = true;
                
                // 設定を事前にキャッシュ
                verboseLogging = TiktokSettings.Instance.VerboseLogging;
                
                // 既存のWebSocketオブジェクトがあれば破棄
                if (ws != null)
                {
                    try
                    {
                        ws.OnOpen -= OnWebSocketOpen;
                        ws.OnMessage -= OnWebSocketMessage;
                        ws.OnError -= OnWebSocketError;
                        ws.OnClose -= OnWebSocketClose;
                        ws = null;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("既存のWebSocketオブジェクトの破棄中にエラーが発生しました: " + ex.Message);
                    }
                }
                
                // 新しいWebSocketオブジェクトを作成
                ws = new WebSocket(fullUrl);

                // イベントハンドラを登録
                ws.OnOpen += OnWebSocketOpen;
                ws.OnMessage += OnWebSocketMessage;
                ws.OnError += OnWebSocketError;
                ws.OnClose += OnWebSocketClose;

                ws.Connect();
            }
            catch (Exception e)
            {
                isConnecting = false;
                string errorMessage = "WebSocketサーバーへの接続に失敗しました: " + e.Message;
                Debug.LogError(errorMessage);
                OnConnectionError?.Invoke(errorMessage);
            }
        }
        
        // WebSocketのOpenイベントハンドラ
        private static void OnWebSocketOpen(object sender, EventArgs e)
        {
            isConnected = true;
            isConnecting = false;
            Debug.Log("WebSocketサーバーに接続しました: " + baseUrl + Username);
            SendApiKeyAndUsername();
        }
        
        // WebSocketのMessageイベントハンドラ
        private static void OnWebSocketMessage(object sender, MessageEventArgs e)
        {
            // メッセージをキューに追加するだけ（メインスレッドでの処理は別途行う）
            lock (messageQueue)
            {
                messageQueue.Enqueue(e.Data);
            }
        }
        
        // WebSocketのErrorイベントハンドラ
        private static void OnWebSocketError(object sender, ErrorEventArgs e)
        {
            string errorMessage = "WebSocketエラー: " + e.Message;
            Debug.LogError(errorMessage);
            OnConnectionError?.Invoke(errorMessage);
        }
        
        // WebSocketのCloseイベントハンドラ
        private static void OnWebSocketClose(object sender, CloseEventArgs e)
        {
            isConnected = false;
            isDisconnecting = false;
            Debug.Log("WebSocketサーバーから切断されました。理由: " + e.Reason);
            
            if (!string.IsNullOrEmpty(e.Reason))
            {
                OnConnectionError?.Invoke("切断理由: " + e.Reason);
            }
            
            // 再接続が要求されていた場合は接続を試みる
            if (reconnectRequested)
            {
                reconnectRequested = false;
                Debug.Log("再接続を実行します。");
                Connect();
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
                    Models.GiftMessage giftMsg = JsonUtility.FromJson<Models.GiftMessage>(message);
                    if (giftMsg != null)
                    {
                        // Debug.Log("ギフトメッセージを受信: " + giftMsg.giftName);
                        OnGiftReceived?.Invoke(giftMsg);
                    }
                }
                else if (message.Contains("\"type\":\"roomUser\"") || message.Contains("\"type\": \"roomUser\""))
                {
                    // 部屋の視聴者情報メッセージの処理
                    Models.RoomUserMessage roomUserMsg = JsonUtility.FromJson<Models.RoomUserMessage>(message);
                    if (roomUserMsg != null)
                    {
                        // Debug.Log($"部屋の視聴者情報を受信: 視聴者数 {roomUserMsg.viewerCount}人");
                        OnRoomUserReceived?.Invoke(roomUserMsg);
                    }
                }
                else if (message.Contains("\"type\":\"like\"") || message.Contains("\"type\": \"like\""))
                {
                    // いいねメッセージの処理
                    Models.LikeMessage likeMsg = JsonUtility.FromJson<Models.LikeMessage>(message);
                    if (likeMsg != null)
                    {
                        // Debug.Log($"いいねメッセージを受信: {likeMsg.nickname}さんが{likeMsg.likeCount}いいねしました (合計: {likeMsg.totalLikeCount})");
                        OnLikeReceived?.Invoke(likeMsg);
                    }
                }
                else if (message.Contains("\"type\":\"chat\"") || message.Contains("\"type\": \"chat\""))
                {
                    // チャットメッセージの処理
                    Models.ChatMessage chatMsg = JsonUtility.FromJson<Models.ChatMessage>(message);
                    if (chatMsg != null)
                    {
                        // Debug.Log($"チャットメッセージを受信: {chatMsg.nickname}さん「{chatMsg.comment}」");
                        OnChatReceived?.Invoke(chatMsg);
                    }
                }
                else if (message.Contains("\"type\":\"share\"") || message.Contains("\"type\": \"share\""))
                {
                    // シェアメッセージの処理
                    Models.ShareMessage shareMsg = JsonUtility.FromJson<Models.ShareMessage>(message);
                    if (shareMsg != null)
                    {
                        Debug.Log($"シェアメッセージを受信: {shareMsg.nickname}さんがライブをシェアしました");
                        OnShareReceived?.Invoke(shareMsg);
                    }
                }
                else if (message.Contains("\"type\":\"follow\"") || message.Contains("\"type\": \"follow\""))
                {
                    // フォローメッセージの処理
                    Models.FollowMessage followMsg = JsonUtility.FromJson<Models.FollowMessage>(message);
                    if (followMsg != null)
                    {
                        Debug.Log($"フォローメッセージを受信: {followMsg.nickname}さんがライブ配信者をフォローしました");
                        OnFollowReceived?.Invoke(followMsg);
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
            // 既に切断済みまたは切断処理中の場合は何もしない
            if (!isConnected && !isConnecting || isDisconnecting)
            {
                Debug.LogWarning("接続がないか、既に切断されています。");
                return;
            }
            
            // 再接続フラグをリセット
            reconnectRequested = false;
            
            if (ws != null)
            {
                try
                {
                    // 切断処理中フラグを立てる
                    isDisconnecting = true;
                    
                    // 接続処理中の場合は、接続処理中フラグをリセット
                    if (isConnecting)
                    {
                        isConnecting = false;
                    }
                    
                    ws.Close();
                    Debug.Log("WebSocketサーバーへの切断を開始しました。");
                }
                catch (Exception ex)
                {
                    isDisconnecting = false;
                    isConnected = false;
                    Debug.LogError("WebSocketの切断中にエラーが発生しました: " + ex.Message);
                }
            }
            else
            {
                isConnected = false;
                isConnecting = false;
                isDisconnecting = false;
                Debug.LogWarning("WebSocketオブジェクトが存在しません。");
            }
        }
        
        public static void Cleanup()
        {
            try
            {
                // 再接続フラグをリセット
                reconnectRequested = false;
                
                if (ws != null)
                {
                    if (isConnected || isConnecting)
                    {
                        try
                        {
                            ws.Close();
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError("WebSocketのクローズ中にエラーが発生しました: " + ex.Message);
                        }
                    }
                    
                    try
                    {
                        ws.OnOpen -= OnWebSocketOpen;
                        ws.OnMessage -= OnWebSocketMessage;
                        ws.OnError -= OnWebSocketError;
                        ws.OnClose -= OnWebSocketClose;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("WebSocketのイベントハンドラ解除中にエラーが発生しました: " + ex.Message);
                    }
                    
                    ws = null;
                }
                
                isConnected = false;
                isConnecting = false;
                isDisconnecting = false;
            }
            catch (Exception ex)
            {
                Debug.LogError("WebSocketのクリーンアップ中にエラーが発生しました: " + ex.Message);
            }
        }
    }
} 