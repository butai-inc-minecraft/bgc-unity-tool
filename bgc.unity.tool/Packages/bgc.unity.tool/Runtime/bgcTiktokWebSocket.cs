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
        // シングルトンインスタンス
        private static BgcTiktokWebSocket _instance;
        
        // シングルトンインスタンスへのアクセサ
        public static BgcTiktokWebSocket Instance
        {
            get
            {
                if (_instance == null)
                {
                    // シーン内にインスタンスがなければ作成
                    GameObject go = new GameObject("BgcTiktokWebSocket");
                    _instance = go.AddComponent<BgcTiktokWebSocket>();
                    DontDestroyOnLoad(go); // シーン遷移時も破棄されないように
                }
                return _instance;
            }
        }
        
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
        
        // サブスクライブ受信時に発火するイベント
        public static event Action<Models.SubscribeMessage> OnSubscribeReceived;
        
        // 接続エラー発生時に発火するイベント
        public static event Action<string> OnConnectionError;
        
        // TiktokWebSocketManagerのインスタンス
        private TiktokWebSocketManager webSocketManager;
        
        // イベントハンドラが登録済みかどうかのフラグ
        private bool eventHandlersRegistered = false;
        
        void Awake()
        {
            // 重複インスタンスのチェック
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            // TiktokWebSocketManagerのインスタンスを取得または作成
            webSocketManager = TiktokWebSocketManager.Instance;
        }
        
        void Start()
        {
            // APIキーを読み込む
            ApiKeyService.LoadApiKey();
            
            // イベントハンドラが未登録の場合のみ登録
            if (!eventHandlersRegistered)
            {
                RegisterEventHandlers();
            }
        }
        
        // イベントハンドラを登録する
        private void RegisterEventHandlers()
        {
            // 既に登録済みの場合は何もしない
            if (eventHandlersRegistered)
            {
                return;
            }
            
            // TiktokWebSocketServiceのイベントをリッスン
            TiktokWebSocketService.OnGiftReceived += HandleGiftReceived;
            TiktokWebSocketService.OnRoomUserReceived += HandleRoomUserReceived;
            TiktokWebSocketService.OnLikeReceived += HandleLikeReceived;
            TiktokWebSocketService.OnChatReceived += HandleChatReceived;
            TiktokWebSocketService.OnShareReceived += HandleShareReceived;
            TiktokWebSocketService.OnFollowReceived += HandleFollowReceived;
            TiktokWebSocketService.OnSubscribeReceived += HandleSubscribeReceived;
            TiktokWebSocketService.OnConnectionError += HandleConnectionError;
            
            eventHandlersRegistered = true;
            Debug.Log("BgcTiktokWebSocket: イベントハンドラを登録しました");
        }
        
        // イベントハンドラを解除する
        private void UnregisterEventHandlers()
        {
            if (!eventHandlersRegistered)
            {
                return;
            }
            
            // イベントハンドラを解除
            TiktokWebSocketService.OnGiftReceived -= HandleGiftReceived;
            TiktokWebSocketService.OnRoomUserReceived -= HandleRoomUserReceived;
            TiktokWebSocketService.OnLikeReceived -= HandleLikeReceived;
            TiktokWebSocketService.OnChatReceived -= HandleChatReceived;
            TiktokWebSocketService.OnShareReceived -= HandleShareReceived;
            TiktokWebSocketService.OnFollowReceived -= HandleFollowReceived;
            TiktokWebSocketService.OnSubscribeReceived -= HandleSubscribeReceived;
            TiktokWebSocketService.OnConnectionError -= HandleConnectionError;
            
            eventHandlersRegistered = false;
            Debug.Log("BgcTiktokWebSocket: イベントハンドラを解除しました");
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
        
        // サブスクライブメッセージを受信したときの処理
        private void HandleSubscribeReceived(Models.SubscribeMessage subscribeMessage)
        {
            // 外部のイベントハンドラに転送
            OnSubscribeReceived?.Invoke(subscribeMessage);
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
            // このインスタンスがシングルトンインスタンスの場合のみクリーンアップ
            if (_instance == this)
            {
                try
                {
                    // イベントハンドラを解除
                    UnregisterEventHandlers();
                    
                    // WebSocketをクリーンアップ
                    TiktokWebSocketService.Cleanup();
                    
                    _instance = null;
                }
                catch (Exception ex)
                {
                    Debug.LogError("BgcTiktokWebSocketのクリーンアップ中にエラーが発生しました: " + ex.Message);
                }
            }
        }
        
        void OnApplicationQuit()
        {
            try
            {
                // アプリケーション終了時にクリーンアップ
                UnregisterEventHandlers();
                TiktokWebSocketService.Cleanup();
            }
            catch (Exception ex)
            {
                Debug.LogError("アプリケーション終了時のクリーンアップ中にエラーが発生しました: " + ex.Message);
            }
        }
    }
}