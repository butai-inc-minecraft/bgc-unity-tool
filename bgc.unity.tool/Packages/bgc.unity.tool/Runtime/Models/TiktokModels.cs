using System;

namespace bgc.unity.tool.Models
{
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
        // @xxxxxxxx
        public string uniqueId;
        // ユーザー名
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
        // webp画像のため、別途対応が必要
        public string giftPictureUrl;
        public string timestamp;
        public string receiverUserId;
        public string profileName;
        public int combo;
        public int ExecutionCount;
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

    // 部屋の視聴者情報メッセージの型を定義
    [Serializable]
    public class RoomUserMessage
    {
        public string type;
        public TopViewer[] topViewers;
        public int viewerCount;
    }

    [Serializable]
    public class TopViewer
    {
        public TikTokUser user;
        public int coinCount;
    }

    [Serializable]
    public class TikTokUser
    {
        public string userId;
        public string secUid;
        public string uniqueId;
        public string nickname;
        public string profilePictureUrl;
        public UserBadge[] userBadges;
        public int[] userSceneTypes;
        public UserDetails userDetails;
        public bool isModerator;
        public bool isNewGifter;
        public bool isSubscriber;
        public int? topGifterRank;
        public int gifterLevel;
        public int teamMemberLevel;
    }

    // いいねメッセージの型を定義
    [Serializable]
    public class LikeMessage
    {
        public string type;
        public int likeCount;
        public int totalLikeCount;
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
        public string createTime;
        public string msgId;
        public string displayType;
        public string label;
    }
    
    // チャットメッセージの型を定義
    [Serializable]
    public class ChatMessage
    {
        public string type;
        public string[] emotes;
        public string comment;
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
    }

    // シェアメッセージの型を定義
    [Serializable]
    public class ShareMessage
    {
        public string type;
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
        public object topGifterRank;
        public int gifterLevel;
        public int teamMemberLevel;
        public string msgId;
        public string createTime;
        public string label;
        public string displayType;
    }

    // フォローメッセージの型を定義
    [Serializable]
    public class FollowMessage
    {
        public string type;
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
        public object topGifterRank;
        public int gifterLevel;
        public int teamMemberLevel;
        public string msgId;
        public string createTime;
        public string displayType;
        public string label;
    }

    // サブスクライブメッセージの型を定義
    [Serializable]
    public class SubscribeMessage
    {
        public string type;
        public int subMonth;
        public int oldSubscribeStatus;
        public int subscribingStatus;
        public string userId;
        public string secUid;
        public string uniqueId;
        public string nickname;
        public string profilePictureUrl;
        public int followRole;
        public UserBadge[] userBadges;
        public UserDetails userDetails;
        public FollowInfo followInfo;
        public bool isModerator;
        public bool isNewGifter;
        public bool isSubscriber;
        public object topGifterRank;
        public int gifterLevel;
        public int teamMemberLevel;
        public string msgId;
        public string createTime;
        public string displayType;
        public string label;
    }
} 