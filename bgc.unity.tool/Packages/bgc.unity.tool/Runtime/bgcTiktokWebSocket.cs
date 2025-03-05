using UnityEngine;
using System;
using bgc.unity.tool.Models;
using bgc.unity.tool.Services;

namespace bgc.unity.tool
{
    // APIキー設定用のクラス
    [Serializable]
    public class ApiKeyConfig
    {
        public string apiKey;
    }

    public class BgcTiktokWebSocket : MonoBehaviour
    {
        // 接続状態
        public static bool IsConnected => TiktokWebSocketService.IsConnected;
        
        // ギフト受信時に発火するイベント
        public static event Action<Models.GiftMessage> OnGiftReceived;
        
        // 部屋の視聴者情報受信時に発火するイベント
        public static event Action<Models.RoomUserMessage> OnRoomUserReceived;
        
        // いいね受信時に発火するイベント
        public static event Action<Models.LikeMessage> OnLikeReceived;
        
        // チャット受信時に発火するイベント
        public static event Action<Models.ChatMessage> OnChatReceived;
        
        // シェア受信時に発火するイベント
        public static event Action<Models.ShareMessage> OnShareReceived;
        
        // フォロー受信時に発火するイベント
        public static event Action<Models.FollowMessage> OnFollowReceived;
        
        // 接続エラー発生時に発火するイベント
        public static event Action<string> OnConnectionError;
        
        // TiktokWebSocketManagerのインスタンス
        private TiktokWebSocketManager webSocketManager;
        
        void Awake()
        {
            // TiktokWebSocketManagerのインスタンスを取得または作成
            webSocketManager = TiktokWebSocketManager.Instance;
        }
        
        void Start()
        {
            // APIキーを読み込む
            ApiKeyService.LoadApiKey();
            
            // TiktokWebSocketServiceのイベントをリッスン
            TiktokWebSocketService.OnGiftReceived += HandleGiftReceived;
            TiktokWebSocketService.OnRoomUserReceived += HandleRoomUserReceived;
            TiktokWebSocketService.OnLikeReceived += HandleLikeReceived;
            TiktokWebSocketService.OnChatReceived += HandleChatReceived;
            TiktokWebSocketService.OnShareReceived += HandleShareReceived;
            TiktokWebSocketService.OnFollowReceived += HandleFollowReceived;
            TiktokWebSocketService.OnConnectionError += HandleConnectionError;
            
        }

        // 外部から username を設定するための関数
        public static void SetUsername(string username)
        {
            TiktokWebSocketManager.Instance.SetUsername(username);
        }

        // ギフトメッセージを受信したときの処理
        private void HandleGiftReceived(Models.GiftMessage giftMessage)
        {
            // 外部のイベントハンドラに転送
            OnGiftReceived?.Invoke(giftMessage);
        }
        
        // 部屋の視聴者情報を受信したときの処理
        private void HandleRoomUserReceived(Models.RoomUserMessage roomUserMessage)
        {
            // 外部のイベントハンドラに転送
            OnRoomUserReceived?.Invoke(roomUserMessage);
        }
        
        // いいねメッセージを受信したときの処理
        private void HandleLikeReceived(Models.LikeMessage likeMessage)
        {
            // 外部のイベントハンドラに転送
            OnLikeReceived?.Invoke(likeMessage);
        }
        
        // チャットメッセージを受信したときの処理
        private void HandleChatReceived(Models.ChatMessage chatMessage)
        {
            // 外部のイベントハンドラに転送
            OnChatReceived?.Invoke(chatMessage);
        }
        
        // シェアメッセージを受信したときの処理
        private void HandleShareReceived(Models.ShareMessage shareMessage)
        {
            // 外部のイベントハンドラに転送
            OnShareReceived?.Invoke(shareMessage);
        }
        
        // フォローメッセージを受信したときの処理
        private void HandleFollowReceived(Models.FollowMessage followMessage)
        {
            // 外部のイベントハンドラに転送
            OnFollowReceived?.Invoke(followMessage);
        }
        
        // 接続エラーが発生したときの処理
        private void HandleConnectionError(string errorMessage)
        {
            Debug.LogError("TikTok接続エラー: " + errorMessage);
            // 外部のイベントハンドラに転送
            OnConnectionError?.Invoke(errorMessage);
        }

        // WebSocketを切断する
        public void Disconnect()
        {
            webSocketManager.Disconnect();
        }
        
        // 再接続を試みる
        public void Reconnect()
        {
            // 接続中または接続処理中の場合は、まず切断する
            if (TiktokWebSocketService.IsConnected || TiktokWebSocketService.IsConnecting)
            {
                Debug.Log("再接続のため、現在の接続を切断します。");
                webSocketManager.Disconnect();
            }
            
            // 切断処理中の場合は、TiktokWebSocketServiceが自動的に再接続するので、
            // ここでは何もしない
            if (TiktokWebSocketService.IsDisconnecting)
            {
                Debug.Log("切断処理中のため、切断完了後に自動的に再接続します。");
                return;
            }
            
            // 接続していない場合は、直接接続する
            Debug.Log("WebSocketサーバーに再接続します。");
            webSocketManager.Connect();
        }

        void OnDestroy()
        {
            try
            {
                // イベントハンドラを解除
                TiktokWebSocketService.OnGiftReceived -= HandleGiftReceived;
                TiktokWebSocketService.OnRoomUserReceived -= HandleRoomUserReceived;
                TiktokWebSocketService.OnLikeReceived -= HandleLikeReceived;
                TiktokWebSocketService.OnChatReceived -= HandleChatReceived;
                TiktokWebSocketService.OnShareReceived -= HandleShareReceived;
                TiktokWebSocketService.OnFollowReceived -= HandleFollowReceived;
                TiktokWebSocketService.OnConnectionError -= HandleConnectionError;
                
                // WebSocketをクリーンアップ
                TiktokWebSocketService.Cleanup();
            }
            catch (Exception ex)
            {
                Debug.LogError("BgcTiktokWebSocketのクリーンアップ中にエラーが発生しました: " + ex.Message);
            }
        }
    }
}