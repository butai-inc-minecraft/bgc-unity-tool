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

    // 受信するギフトメッセージの型を定義
    [Serializable]
    public class GiftMessage
    {
        public string type;
        public int giftId;
        public int repeatCount;
        public string groupId;
        public string userId;
        public string secUid;
        public string uniqueId;
        public string nickname;
        public string profilePictureUrl;
        public int followRole;
        public UserBadge[] userBadges;
        public int[] userSceneTypes;
        public UserDetails userDetails;
        public FollowInfo followInfo;
        public bool isModerator;
        public bool isNewGifter;
        public bool isSubscriber;
        public int? topGifterRank;
        public int gifterLevel;
        public int teamMemberLevel;
        public string msgId;
        public string createTime;
        public string displayType;
        public string label;
        public bool repeatEnd;
        public Gift gift;
        public string describe;
        public int giftType;
        public int diamondCount;
        public string giftName;
        public string giftPictureUrl;
        public string timestamp;
        public string receiverUserId;
        public string profileName;
        public int combo;
    }

    [Serializable]
    public class UserBadge
    {
        public string type;
        public string privilegeId;
        public int level;
        public int badgeSceneType;
    }

    [Serializable]
    public class UserDetails
    {
        public string createTime;
        public string bioDescription;
        public string[] profilePictureUrls;
    }

    [Serializable]
    public class FollowInfo
    {
        public int followingCount;
        public int followerCount;
        public int followStatus;
        public int pushStatus;
    }

    [Serializable]
    public class Gift
    {
        public int gift_id;
        public int repeat_count;
        public int repeat_end;
        public int gift_type;
    }

    public class BgcTiktokWebSocket : MonoBehaviour
    {
        // 接続状態
        public static bool IsConnected => TiktokWebSocketService.IsConnected;
        
        // ギフト受信時に発火するイベント
        public static event Action<GiftMessage> OnGiftReceived;
        
        // 部屋の視聴者情報受信時に発火するイベント
        public static event Action<RoomUserMessage> OnRoomUserReceived;
        
        // いいね受信時に発火するイベント
        public static event Action<LikeMessage> OnLikeReceived;
        
        // チャット受信時に発火するイベント
        public static event Action<ChatMessage> OnChatReceived;
        
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
            TiktokWebSocketService.OnConnectionError += HandleConnectionError;
            
            // WebSocketManagerを使用して接続
            webSocketManager.Connect();
        }

        // 外部から username を設定するための関数
        public static void SetUsername(string username)
        {
            TiktokWebSocketManager.Instance.SetUsername(username);
        }

        // ギフトメッセージを受信したときの処理
        private void HandleGiftReceived(GiftMessage giftMessage)
        {
            // 外部のイベントハンドラに転送
            OnGiftReceived?.Invoke(giftMessage);
        }
        
        // 部屋の視聴者情報を受信したときの処理
        private void HandleRoomUserReceived(RoomUserMessage roomUserMessage)
        {
            // 外部のイベントハンドラに転送
            OnRoomUserReceived?.Invoke(roomUserMessage);
        }
        
        // いいねメッセージを受信したときの処理
        private void HandleLikeReceived(LikeMessage likeMessage)
        {
            // 外部のイベントハンドラに転送
            OnLikeReceived?.Invoke(likeMessage);
        }
        
        // チャットメッセージを受信したときの処理
        private void HandleChatReceived(ChatMessage chatMessage)
        {
            // 外部のイベントハンドラに転送
            OnChatReceived?.Invoke(chatMessage);
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
            webSocketManager.Disconnect();
            webSocketManager.Connect();
        }

        void OnDestroy()
        {
            // イベントハンドラを解除
            TiktokWebSocketService.OnGiftReceived -= HandleGiftReceived;
            TiktokWebSocketService.OnRoomUserReceived -= HandleRoomUserReceived;
            TiktokWebSocketService.OnLikeReceived -= HandleLikeReceived;
            TiktokWebSocketService.OnChatReceived -= HandleChatReceived;
            TiktokWebSocketService.OnConnectionError -= HandleConnectionError;
            
            // WebSocketをクリーンアップ
            TiktokWebSocketService.Cleanup();
        }
    }
}