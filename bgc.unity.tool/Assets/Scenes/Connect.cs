using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using bgc.unity.tool;
using bgc.unity.tool.Services;
using bgc.unity.tool.Models;
// using bgc.unity.tool.Utils;

public class Handler : MonoBehaviour
{
    // 同一 GameObject にアタッチされた BgcTiktokWebSocket コンポーネントを取得
    private BgcTiktokWebSocket bgcTiktokWebSocket;
    
    // UI の InputField（インスペクターでアサイン）
    [SerializeField] private InputField usernameInputField;
    
    // オプション：ボタンを使う場合はインスペクターでアサイン
    [SerializeField] private Button connectButton;
    
    // エラーメッセージを表示するテキスト
    [SerializeField] private Text errorMessageText;
    
    // 接続状態を表示するテキスト
    [SerializeField] private Text connectionStatusText;
    
    // チャットメッセージを表示するテキスト（オプション）
    [SerializeField] private Text chatMessageText;
    
    // 視聴者数を表示するテキスト
    [SerializeField] private Text viewerCountText;
    
    // いいね数を表示するテキスト
    [SerializeField] private Text likeCountText;
    
    // コメントログとギフトログのUI
    [Header("コメントログとギフトログ")]
    [SerializeField] private Transform chatLogContainer; // コメントログの親オブジェクト
    [SerializeField] private Transform giftLogContainer; // ギフトログの親オブジェクト
    [SerializeField] private GameObject chatItemPrefab;  // コメントアイテムのプレハブ
    [SerializeField] private GameObject giftItemPrefab;  // ギフトアイテムのプレハブ
    [SerializeField] private int maxLogItems = 20;       // 表示する最大ログアイテム数
    [SerializeField] private ScrollRect chatScrollRect;  // コメントログのScrollRect
    [SerializeField] private ScrollRect giftScrollRect;  // ギフトログのScrollRect
    
    // 表示する最大チャットメッセージ数
    [SerializeField] private int maxChatMessages = 5;
    
    // チャットメッセージのリスト
    private List<string> recentChatMessages = new List<string>();
    
    // ユーザーごとの累積いいね数を記録する辞書
    private Dictionary<string, int> userTotalLikes = new Dictionary<string, int>();
    
    // 現在の視聴者数
    private int currentViewerCount = 0;
    
    // コメントログとギフトログのアイテムリスト
    private List<GameObject> chatLogItems = new List<GameObject>();
    private List<GameObject> giftLogItems = new List<GameObject>();
    
    // ギフトのストリーク（連続送信）を追跡するための辞書
    private Dictionary<string, GameObject> giftStreaks = new Dictionary<string, GameObject>();

    // 接続状態表示用の変数
    [Header("接続状態表示")]
    [SerializeField] private Image connectionStatusIndicator;
    [SerializeField] private Color notConnectedColor = Color.white;     // 接続していない時：白
    [SerializeField] private Color connectedColor = new Color(1.0f, 0.5f, 0.0f); // 接続中：オレンジ
    [SerializeField] private Color disconnectingColor = Color.red;      // 切断中：赤
    [SerializeField] private float statusUpdateInterval = 0.5f;
    private float statusUpdateTimer = 0f;

    private void Awake()
    {
        // new 演算子は使えないため、GetComponent で取得する
        bgcTiktokWebSocket = GetComponent<BgcTiktokWebSocket>();
        if(bgcTiktokWebSocket == null)
        {
            Debug.LogError("BgcTiktokWebSocket コンポーネントが見つかりません。");
            // 初期化を試みる
            bgcTiktokWebSocket = gameObject.AddComponent<BgcTiktokWebSocket>();
            if (bgcTiktokWebSocket != null)
            {
                Debug.Log("BgcTiktokWebSocket コンポーネントを新たに追加しました。");
            }
            else
            {
                Debug.LogError("BgcTiktokWebSocket コンポーネントの追加に失敗しました。");
            }
        }
        
        // ScrollRectのContentフィールドをチェック
        if (chatScrollRect != null && chatScrollRect.content == null)
        {
            Debug.LogError("chatScrollRectのContentフィールドが設定されていません。InspectorでContentフィールドを設定してください。");
        }
        
        if (giftScrollRect != null && giftScrollRect.content == null)
        {
            Debug.LogError("giftScrollRectのContentフィールドが設定されていません。InspectorでContentフィールドを設定してください。");
        }
        
        // エラーメッセージテキストを初期化
        if (errorMessageText != null)
        {
            errorMessageText.text = "";
        }
        
        // 接続状態テキストを初期化
        UpdateConnectionStatus();
        
        // チャットメッセージテキストを初期化
        if (chatMessageText != null)
        {
            chatMessageText.text = "";
        }
        
        // 視聴者数テキストを初期化
        UpdateViewerCountUI();
        
        // いいね数テキストを初期化
        UpdateLikeCountUI(0);
    }

    private void Start()
    {
        // イベントハンドラを登録
        BgcTiktokWebSocket.OnConnectionError += HandleConnectionError;
        BgcTiktokWebSocket.OnLikeReceived += HandleLikeReceived;
        BgcTiktokWebSocket.OnChatReceived += HandleChatReceived;
        BgcTiktokWebSocket.OnRoomUserReceived += HandleRoomUserReceived;
        BgcTiktokWebSocket.OnGiftReceived += HandleGiftReceived;
        BgcTiktokWebSocket.OnShareReceived += HandleShareReceived;
        BgcTiktokWebSocket.OnFollowReceived += HandleFollowReceived;
        
        // ボタンがある場合は、リスナー登録を行う
        if (connectButton != null)
        {
            connectButton.onClick.AddListener(OnConnectButtonClicked);
        }

        // デフォルトユーザー名を取得
        string defaultUsername = bgc.unity.tool.ScriptableObjects.TiktokSettings.Instance.DefaultUsername;
        
        // デフォルトユーザー名が設定されている場合は、InputFieldに設定する
        if (!string.IsNullOrEmpty(defaultUsername) && defaultUsername != "default" && usernameInputField != null)
        {
            usernameInputField.text = defaultUsername;
            Debug.Log("デフォルトユーザー名を設定: " + defaultUsername);
        }

        // 接続状態の更新を開始
        StartCoroutine(UpdateConnectionStatusRoutine());
    }
    
    private void Update()
    {
        // 接続状態の更新タイマー
        statusUpdateTimer += Time.deltaTime;
        if (statusUpdateTimer >= statusUpdateInterval)
        {
            UpdateConnectionStatus();
            statusUpdateTimer = 0f;
        }
    }

    // ボタンから呼び出される場合の処理
    private void OnConnectButtonClicked()
    {
        Debug.Log("Connect ボタンが押されました！");
        if (usernameInputField != null)
        {
            string username = usernameInputField.text;
            Debug.Log("username: " + username);
            ConnectToWebSocket(username);
        }
        else
        {
            Debug.LogError("usernameInputField がアサインされていません。");
            ShowErrorMessage("ユーザー名入力フィールドがアサインされていません。");
        }
    }

    // 指定された username で接続を試みる
    private void ConnectToWebSocket(string username)
    {
        // エラーメッセージをクリア
        ClearErrorMessage();
        
        if (string.IsNullOrEmpty(username))
        {
            Debug.LogWarning("Username が空です。接続できません。");
            ShowErrorMessage("ユーザー名を入力してください。");
            return;
        }
        
        // TiktokWebSocketManagerを使用して接続
        TiktokWebSocketManager.Instance.SetUsername(username);
        TiktokWebSocketManager.Instance.Connect();
        Debug.Log("接続を開始しました。");
    }

    // 必要に応じて切断処理を呼び出す
    private void DisconnectFromWebSocket()
    {
        TiktokWebSocketManager.Instance.Disconnect();
        Debug.Log("切断しました。");
        UpdateConnectionStatus();
    }
    
    // 接続エラーを処理する
    private void HandleConnectionError(string errorMessage)
    {
        ShowErrorMessage(errorMessage);
    }
    
    // いいねメッセージを受信したときの処理
    private void HandleLikeReceived(LikeMessage likeMessage)
    {
        string userId = likeMessage.userId;
        string nickname = likeMessage.nickname;
        int likeCount = likeMessage.likeCount;
        int totalLikeCount = likeMessage.totalLikeCount;
        
        // todo - ユーザーのいいねごとのXXXを実装したい場合は、辞書にuserIdをキーにしていいね数を記録する
        // いいね情報をログに表示
        Debug.Log($"👍 {nickname}さんから{likeCount}いいねを受け取りました！ 累計: {totalLikeCount}");
        
        // いいね数のUI更新（TikTokから受け取った累計いいね数を表示）
        UpdateLikeCountUI(totalLikeCount);
    }
    
    // チャットメッセージを受信したときの処理
    private void HandleChatReceived(ChatMessage chatMessage)
    {
        string userId = chatMessage.userId;
        string nickname = chatMessage.nickname;
        string comment = chatMessage.comment;
        string uniqueId = chatMessage.uniqueId;

        // チャットメッセージをログに表示
        Debug.Log($"💬 {nickname} (@{userId}): {comment}");
        
        // チャットメッセージをリストに追加
        AddChatMessage($"{nickname} (@{userId}): {comment}");
        
        // チャットメッセージのUI更新
        UpdateChatUI();
        
        // コメントログに追加
        AddChatLogItem(userId, nickname, uniqueId, comment);
        
        // 特定のキーワードに反応する例
        if (comment.Contains("おめでとう") || comment.Contains("congratulations"))
        {
            Debug.Log($"🎊 {nickname}さんからお祝いのメッセージが届きました！");
            // ここにお祝いメッセージを受信したときの処理を追加
        }
        
        if (comment.Contains("質問") || comment.Contains("question"))
        {
            Debug.Log($"❓ {nickname}さんから質問が届きました！");
            // ここに質問を受信したときの処理を追加
        }
    }
    
    // ギフトメッセージを受信したときの処理
    private void HandleGiftReceived(GiftMessage giftMessage)
    {
        string userId = giftMessage.userId;
        string nickname = giftMessage.nickname;
        string giftName = giftMessage.giftName;
        int giftId = giftMessage.giftId;
        int diamondCount = giftMessage.diamondCount;
        int repeatCount = giftMessage.repeatCount;
        bool repeatEnd = giftMessage.repeatEnd;
        int giftType = giftMessage.giftType;

        // ギフトアイコン
        string iconUrl = giftMessage.giftPictureUrl;
        
        // ギフト情報をログに表示
        Debug.Log($"🎁 {nickname}さんから{giftName}（ID:{giftId}, {diamondCount}ダイヤ）を{repeatCount}回受け取りました！ repeatEnd: {repeatEnd}, giftType: {giftType}");

        // バラが投げられた時
        if(giftName == "Rose"){
            // 🌹🌹 とログに表示
            Debug.Log("🌹🌹");
        }

        // 1コインのギフトの時
        if(diamondCount == 1){
            // 💰
            Debug.Log("💰: 1コインギフト");
        }
        // ストリークIDを生成（ユーザーIDとギフトIDの組み合わせ）
        string streakId = userId + "_" + giftId;
        
        // ギフトログに追加または更新
        GameObject newGiftItem = AddGiftLogItem(userId, nickname, giftName, giftId, diamondCount, repeatCount, repeatEnd, iconUrl);
        
        // repeatEndがtrueの場合の処理
        if (repeatEnd)
        {
            // 同じuserId_giftIdの組み合わせで赤文字のギフト（repeatEnd:false）のみを探して削除
            List<GameObject> itemsToRemove = new List<GameObject>();
            
            foreach (GameObject item in giftLogItems)
            {
                // アイテムが既に削除されている場合または今追加したアイテムの場合はスキップ
                if (item == null || item == newGiftItem) continue;
                
                // GiftItemPrefabコンポーネントを取得
                GiftItemPrefab giftItemComponent = item.GetComponent<GiftItemPrefab>();
                if (giftItemComponent != null && !giftItemComponent.IsRepeatEnded && giftItemComponent.GetStreakId() == streakId)
                {
                    // 同じstreakIdで赤文字のギフト（repeatEnd:false）を削除リストに追加
                    itemsToRemove.Add(item);
                    Debug.Log($"赤文字のギフト（repeatEnd:false）を削除リストに追加: {streakId}");
                }
            }
            
            // 削除リストのアイテムを削除
            foreach (GameObject itemToRemove in itemsToRemove)
            {
                giftLogItems.Remove(itemToRemove);
                Destroy(itemToRemove);
                Debug.Log($"赤文字のギフトを削除しました");
            }
            
            // ストリーク辞書からも削除
            if (giftStreaks.ContainsKey(streakId))
            {
                Debug.Log($"ストリーク終了: {streakId} - 辞書から削除します");
                giftStreaks.Remove(streakId);
            }
        }
    }
    
    // チャットメッセージをリストに追加
    private void AddChatMessage(string message)
    {
        recentChatMessages.Add(message);
        
        // 最大メッセージ数を超えた場合、古いメッセージを削除
        while (recentChatMessages.Count > maxChatMessages)
        {
            recentChatMessages.RemoveAt(0);
        }
    }
    
    // チャットメッセージのUI更新
    private void UpdateChatUI()
    {
        if (chatMessageText != null)
        {
            // リスト内のすべてのメッセージを結合
            string allMessages = string.Join("\n", recentChatMessages);
            chatMessageText.text = allMessages;
        }
    }
    
    // いいね数のUI更新
    private void UpdateLikeCountUI(int totalLikeCount)
    {
        if (likeCountText != null)
        {
            likeCountText.text = $"いいね数: {totalLikeCount}";
            Debug.Log($"累計いいね数を更新: {totalLikeCount}");
        }
    }
    
    // エラーメッセージを表示
    private void ShowErrorMessage(string message)
    {
        if (errorMessageText != null)
        {
            errorMessageText.text = "エラー: " + message;
            errorMessageText.color = disconnectingColor; // 赤色を使用
        }
        
        Debug.LogError("エラー: " + message);
    }
    
    // エラーメッセージをクリアする
    private void ClearErrorMessage()
    {
        if (errorMessageText != null)
        {
            errorMessageText.text = "";
        }
    }
    
    // 接続状態を更新する
    private void UpdateConnectionStatus()
    {
        if (connectionStatusText != null)
        {
            string statusText;
            Color statusColor;
            
            if (TiktokWebSocketService.IsConnected || TiktokWebSocketService.IsConnecting)
            {
                // 接続中または接続処理中はオレンジ
                statusText = "接続状態: 接続中";
                statusColor = connectedColor;
            }
            else if (TiktokWebSocketService.IsDisconnecting)
            {
                // 切断中は赤
                statusText = "接続状態: 切断中";
                statusColor = disconnectingColor;
            }
            else
            {
                // 未接続は白
                statusText = "接続状態: 未接続";
                statusColor = notConnectedColor;
            }
            
            connectionStatusText.text = statusText;
            connectionStatusText.color = statusColor;
        }
        
        // 接続状態インジケーターの更新
        if (connectionStatusIndicator != null)
        {
            Color indicatorColor;
            
            if (TiktokWebSocketService.IsConnected || TiktokWebSocketService.IsConnecting)
            {
                // 接続中または接続処理中はオレンジ
                indicatorColor = connectedColor;
            }
            else if (TiktokWebSocketService.IsDisconnecting)
            {
                // 切断中は赤
                indicatorColor = disconnectingColor;
            }
            else
            {
                // 未接続は白
                indicatorColor = notConnectedColor;
            }
            
            connectionStatusIndicator.color = indicatorColor;
        }
        
        // 接続ボタンのテキスト更新
        if (connectButton != null)
        {
            Text buttonText = connectButton.GetComponentInChildren<Text>();
            if (buttonText != null)
            {
                buttonText.text = TiktokWebSocketService.IsConnected ? "切断" : "接続";
            }
        }
    }
    
    // すべてのユーザーのいいね数をリセット
    public void ResetAllUserLikes()
    {
        userTotalLikes.Clear();
        Debug.Log("すべてのユーザーのいいね数をリセットしました。");
    }
    
    // チャットメッセージをクリア
    public void ClearChatMessages()
    {
        recentChatMessages.Clear();
        UpdateChatUI();
        Debug.Log("チャットメッセージをクリアしました。");
    }
    
    // 部屋の視聴者情報を受信したときの処理
    private void HandleRoomUserReceived(RoomUserMessage roomUserMessage)
    {
        // 視聴者数を更新
        currentViewerCount = roomUserMessage.viewerCount;
        
        // 視聴者数のUI更新
        UpdateViewerCountUI();
        
        // ログに表示
        Debug.Log($"👁 視聴者数: {currentViewerCount}人");
    }
    
    // 視聴者数のUI更新
    private void UpdateViewerCountUI()
    {
        if (viewerCountText != null)
        {
            viewerCountText.text = $"視聴者数: {currentViewerCount}人";
        }
    }
    
    // コメントログにアイテムを追加
    private void AddChatLogItem(string userId, string nickname, string uniqueId, string comment)
    {
        if (chatLogContainer == null)
        {
            Debug.LogError("コメントログの親オブジェクトがアサインされていません。Inspector でアサインしてください。");
            return;
        }
        
        if (chatItemPrefab == null)
        {
            Debug.LogError("コメントアイテムのプレハブがアサインされていません。Inspector でアサインしてください。");
            return;
        }
        
        // デバッグ情報
        Debug.Log($"コメント追加: {nickname} (@{userId}), {comment}");
        Debug.Log($"chatLogContainer: {chatLogContainer.name}, 子オブジェクト数: {chatLogContainer.childCount}");
        if (chatScrollRect != null && chatScrollRect.content != null)
        {
            Debug.Log($"chatScrollRect.content: {chatScrollRect.content.name}, サイズ: {chatScrollRect.content.rect.size}");
        }
        
        // プレハブからコメントアイテムを生成
        GameObject chatItem = Instantiate(chatItemPrefab, chatLogContainer);
        
        // アイテムが非アクティブの場合はアクティブにする
        if (!chatItem.activeSelf)
        {
            chatItem.SetActive(true);
        }
        
        // ChatItemPrefabコンポーネントがあれば、それを使用
        ChatItemPrefab chatItemComponent = chatItem.GetComponent<ChatItemPrefab>();
        if (chatItemComponent != null)
        {
            chatItemComponent.SetChatInfo(uniqueId, nickname, comment);
            Debug.Log($"ChatItemPrefabコンポーネントを使用: {nickname} (@{uniqueId}), {comment}");
            
            // テキストの色を強制的に設定
            Text[] allTexts = chatItem.GetComponentsInChildren<Text>();
            foreach (Text text in allTexts)
            {
                // 黒色に設定
                text.color = Color.black;
                // フォントサイズを確認
                if (text.fontSize < 12)
                {
                    text.fontSize = 14;
                }
            }
        }
        else
        {
            // 従来の方法（コンポーネントがない場合）
            Text[] texts = chatItem.GetComponentsInChildren<Text>();
            Debug.Log($"テキストコンポーネント数: {texts.Length}");
            
            if (texts.Length >= 3)
            {
                // 3つ以上のテキストがある場合は、nickname、uniqueId、commentを別々に表示
                texts[0].text = nickname;
                texts[1].text = "@" + userId;
                texts[2].text = comment;
                
                // テキストの色を強制的に設定
                texts[0].color = Color.black;
                texts[1].color = Color.black;
                texts[2].color = Color.black;
                
                // フォントサイズを確認
                if (texts[0].fontSize < 12) texts[0].fontSize = 14;
                if (texts[1].fontSize < 12) texts[1].fontSize = 14;
                if (texts[2].fontSize < 12) texts[2].fontSize = 14;
                
                Debug.Log($"テキスト1(nickname): {texts[0].text}, 色: {texts[0].color}, フォントサイズ: {texts[0].fontSize}");
                Debug.Log($"テキスト2(uniqueId): {texts[1].text}, 色: {texts[1].color}, フォントサイズ: {texts[1].fontSize}");
                Debug.Log($"テキスト3(comment): {texts[2].text}, 色: {texts[2].color}, フォントサイズ: {texts[2].fontSize}");
            }
            else if (texts.Length == 2)
            {
                texts[0].text = nickname + " (@" + userId + "):";
                texts[1].text = comment;
                // テキストの色を強制的に設定
                texts[0].color = Color.black;
                texts[1].color = Color.black;
                // フォントサイズを確認
                if (texts[0].fontSize < 12) texts[0].fontSize = 14;
                if (texts[1].fontSize < 12) texts[1].fontSize = 14;
                
                Debug.Log($"テキスト1: {texts[0].text}, 色: {texts[0].color}, フォントサイズ: {texts[0].fontSize}, アクティブ: {texts[0].gameObject.activeSelf}");
                Debug.Log($"テキスト2: {texts[1].text}, 色: {texts[1].color}, フォントサイズ: {texts[1].fontSize}, アクティブ: {texts[1].gameObject.activeSelf}");
            }
            else if (texts.Length == 1)
            {
                texts[0].text = nickname + " (@" + userId + "): " + comment;
                // テキストの色を強制的に設定
                texts[0].color = Color.black;
                // フォントサイズを確認
                if (texts[0].fontSize < 12) texts[0].fontSize = 14;
                
                Debug.Log($"テキスト: {texts[0].text}, 色: {texts[0].color}, フォントサイズ: {texts[0].fontSize}, アクティブ: {texts[0].gameObject.activeSelf}");
            }
            else
            {
                Debug.LogError("テキストコンポーネントが見つかりません。");
            }
        }
        
        // リストに追加
        chatLogItems.Add(chatItem);
        
        // 最大数を超えた場合、古いアイテムを削除
        if (chatLogItems.Count > maxLogItems)
        {
            GameObject oldestItem = chatLogItems[0];
            chatLogItems.RemoveAt(0);
            Destroy(oldestItem);
        }
        
        // レイアウトを更新
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)chatLogContainer);
        
        // 自動スクロール - 一番下に移動
        if (chatScrollRect != null)
        {
            if (chatScrollRect.content != null)
            {
                // 次のフレームでスクロール位置を更新
                StartCoroutine(ScrollToBottomNextFrame(chatScrollRect));
            }
            else
            {
                Debug.LogError("chatScrollRect.contentがnullです。ScrollRectのContentフィールドを設定してください。");
            }
        }
    }
    
    // 次のフレームでスクロール位置を更新するコルーチン
    private IEnumerator ScrollToBottomNextFrame(ScrollRect scrollRect)
    {
        yield return null; // 次のフレームまで待機
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f; // 0が一番下、1が一番上
        
        // さらに1フレーム待機して再度スクロール位置を更新（より確実にするため）
        yield return null;
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
    }
    
    // ギフトログにアイテムを追加または更新
    private GameObject AddGiftLogItem(string userId, string username, string giftName, int giftId, int diamonds, int repeatCount, bool repeatEnd, string giftIconUrl)
    {
        if (giftLogContainer == null)
        {
            Debug.LogError("ギフトログの親オブジェクトがアサインされていません。Inspector でアサインしてください。");
            return null;
        }
        
        if (giftItemPrefab == null)
        {
            Debug.LogError("ギフトアイテムのプレハブがアサインされていません。Inspector でアサインしてください。");
            return null;
        }
        
        // ストリークIDを生成（ユーザーIDとギフトIDの組み合わせ）
        string streakId = userId + "_" + giftId;
        
        // 既存のストリークアイテムを確認
        GameObject giftItem;
        bool isNewItem = !giftStreaks.TryGetValue(streakId, out giftItem);
        
        // 新しいアイテムの場合のみ作成
        if (isNewItem)
        {
            // 新しいアイテムを作成
            giftItem = Instantiate(giftItemPrefab, giftLogContainer);
            
            // ストリーク辞書に追加（repeatEndがtrueでも一時的に追加）
            if (!repeatEnd)
            {
                giftStreaks[streakId] = giftItem;
            }
        }
        
        // アイテムが非アクティブの場合はアクティブにする
        if (giftItem != null && !giftItem.activeSelf)
        {
            giftItem.SetActive(true);
        }
        
        // 新しいアイテムの場合はリストに追加
        if (isNewItem)
        {
            // リストに追加
            giftLogItems.Add(giftItem);
            
            // 最大数を超えた場合、古いアイテムを削除
            if (giftLogItems.Count > maxLogItems)
            {
                GameObject oldestItem = giftLogItems[0];
                giftLogItems.RemoveAt(0);
                
                // 削除するアイテムがストリーク中のアイテムなら辞書からも削除
                foreach (var kvp in new Dictionary<string, GameObject>(giftStreaks))
                {
                    if (kvp.Value == oldestItem)
                    {
                        giftStreaks.Remove(kvp.Key);
                        break;
                    }
                }
                
                Destroy(oldestItem);
            }
        }
        
        // GiftItemPrefabコンポーネントがあれば、それを使用
        GiftItemPrefab giftItemComponent = giftItem.GetComponent<GiftItemPrefab>();
        if (giftItemComponent != null)
        {
            // ユーザーIDとギフトIDを設定（新規アイテムでも既存アイテムでも必ず設定）
            giftItemComponent.SetUserAndGiftId(userId, giftId);
            
            // ギフトアイコンがある場合は、画像をダウンロードして設定
            if (!string.IsNullOrEmpty(giftIconUrl))
            {
                StartCoroutine(DownloadGiftIcon(giftIconUrl, (sprite) => {
                    if (sprite != null)
                    {
                        Debug.Log($"GiftItemPrefabコンポーネントにギフトアイコンを設定します");
                        giftItemComponent.SetGiftInfo(username, giftName, diamonds, repeatCount, sprite, repeatEnd);
                        
                        // 画像が設定されたことを確認
                        Image giftIcon = giftItemComponent.GetGiftIcon();
                        if (giftIcon != null)
                        {
                            Debug.Log($"ギフトアイコンが正しく設定されました: {giftIcon.sprite != null}, サイズ: {sprite.rect.width}x{sprite.rect.height}");
                            giftIcon.gameObject.SetActive(true);
                            giftIcon.preserveAspect = true; // アスペクト比を保持
                        }
                        else
                        {
                            Debug.LogError("ギフトアイコンのImageコンポーネントが見つかりません");
                        }
                        
                        // レイアウトを更新
                        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)giftItem.transform);
                        
                        // アイコンロード後にもスクロールを一番下に移動
                        if (giftScrollRect != null && giftScrollRect.content != null)
                        {
                            StartCoroutine(ScrollToBottomNextFrame(giftScrollRect));
                        }
                    }
                    else
                    {
                        Debug.LogError($"ギフトアイコンのダウンロードに失敗しました: {giftIconUrl}");
                        giftItemComponent.SetGiftInfo(username, giftName, diamonds, repeatCount, null, repeatEnd);
                    }
                }));
            }
            else
            {
                giftItemComponent.SetGiftInfo(username, giftName, diamonds, repeatCount, null, repeatEnd);
            }
            
            Debug.Log($"GiftItemPrefabコンポーネントを使用: {username}, {giftName}, {diamonds}, {repeatCount}, アイコンURL: {giftIconUrl}, 新規アイテム: {isNewItem}, ストリーク終了: {repeatEnd}");
            
            // テキストの色を強制的に設定
            Text[] allTexts = giftItem.GetComponentsInChildren<Text>();
            foreach (Text text in allTexts)
            {
                // 黒色に設定
                text.color = Color.black;
                // フォントサイズを確認
                if (text.fontSize < 12)
                {
                    text.fontSize = 14;
                }
            }
        }
        else
        {
            // 従来の方法（コンポーネントがない場合）
            Text[] texts = giftItem.GetComponentsInChildren<Text>();
            Debug.Log($"ギフトテキストコンポーネント数: {texts.Length}");
            
            if (texts.Length >= 3)
            {
                texts[0].text = username + ":";
                texts[1].text = "Sent " + giftName;
                texts[2].text = "Repeat: x" + repeatCount + "\nCost: " + diamonds + " Diamonds";
                
                // テキストの色を強制的に設定
                texts[0].color = Color.black;
                texts[1].color = Color.black;
                texts[2].color = Color.black;
                // フォントサイズを確認
                if (texts[0].fontSize < 12) texts[0].fontSize = 14;
                if (texts[1].fontSize < 12) texts[1].fontSize = 14;
                if (texts[2].fontSize < 12) texts[2].fontSize = 14;
                
                Debug.Log($"ギフトテキスト1: {texts[0].text}, 色: {texts[0].color}, フォントサイズ: {texts[0].fontSize}, アクティブ: {texts[0].gameObject.activeSelf}");
                Debug.Log($"ギフトテキスト2: {texts[1].text}, 色: {texts[1].color}, フォントサイズ: {texts[1].fontSize}, アクティブ: {texts[1].gameObject.activeSelf}");
                Debug.Log($"ギフトテキスト3: {texts[2].text}, 色: {texts[2].color}, フォントサイズ: {texts[2].fontSize}, アクティブ: {texts[2].gameObject.activeSelf}");
                
                // ギフトアイコンがある場合は、画像をダウンロードして設定
                if (!string.IsNullOrEmpty(giftIconUrl))
                {
                    // ギフトアイコン用のImageコンポーネントを探す
                    Image giftIconImage = null;
                    
                    // 子オブジェクトのImageコンポーネントを検索
                    Image[] images = giftItem.GetComponentsInChildren<Image>(true);
                    foreach (Image img in images)
                    {
                        // 名前に「Icon」または「Image」が含まれるコンポーネントを優先
                        if (img.gameObject.name.Contains("Icon") || img.gameObject.name.Contains("Image"))
                        {
                            giftIconImage = img;
                            Debug.Log($"ギフトアイコン用のImageコンポーネントを見つけました: {img.gameObject.name}");
                            break;
                        }
                    }
                    
                    // 特定の名前のImageが見つからなかった場合は最初のImageを使用
                    if (giftIconImage == null && images.Length > 0)
                    {
                        giftIconImage = images[0];
                        Debug.Log($"最初のImageコンポーネントをギフトアイコンとして使用します: {giftIconImage.gameObject.name}");
                    }
                    
                    if (giftIconImage != null)
                    {
                        StartCoroutine(DownloadGiftIcon(giftIconUrl, (sprite) => {
                            if (sprite != null)
                            {
                                Debug.Log($"ギフトアイコンをImageに設定します: {giftIconImage.gameObject.name}");
                                giftIconImage.sprite = sprite;
                                giftIconImage.preserveAspect = true; // アスペクト比を保持
                                giftIconImage.gameObject.SetActive(true);
                                
                                // レイアウトを更新
                                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)giftItem.transform);
                                
                                // アイコンロード後にもスクロールを一番下に移動
                                if (giftScrollRect != null && giftScrollRect.content != null)
                                {
                                    StartCoroutine(ScrollToBottomNextFrame(giftScrollRect));
                                }
                            }
                            else
                            {
                                Debug.LogError($"ギフトアイコンのダウンロードに失敗しました: {giftIconUrl}");
                                giftIconImage.gameObject.SetActive(false);
                            }
                        }));
                    }
                    else
                    {
                        Debug.LogError("ギフトアイコン用のImageコンポーネントが見つかりません");
                    }
                }
            }
            else if (texts.Length == 1)
            {
                texts[0].text = username + ": Sent " + giftName + " (x" + repeatCount + ", " + diamonds + " Diamonds)";
                
                // テキストの色を強制的に設定
                texts[0].color = Color.black;
                // フォントサイズを確認
                if (texts[0].fontSize < 12) texts[0].fontSize = 14;
                
                Debug.Log($"ギフトテキスト: {texts[0].text}, 色: {texts[0].color}, フォントサイズ: {texts[0].fontSize}, アクティブ: {texts[0].gameObject.activeSelf}");
            }
            else
            {
                Debug.LogError("ギフトテキストコンポーネントが見つかりません。");
            }
        }
        
        // 常にレイアウトを更新し、スクロールを一番下に移動
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)giftLogContainer);
        
        // 自動スクロール - 一番下に移動
        if (giftScrollRect != null)
        {
            if (giftScrollRect.content != null)
            {
                // 次のフレームでスクロール位置を更新
                StartCoroutine(ScrollToBottomNextFrame(giftScrollRect));
            }
            else
            {
                Debug.LogError("giftScrollRect.contentがnullです。ScrollRectのContentフィールドを設定してください。");
            }
        }
        
        return giftItem;
    }
    
    // ギフトアイコンをダウンロードするコルーチン
    private IEnumerator DownloadGiftIcon(string url, System.Action<Sprite> callback)
    {
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogError("ギフトアイコンのURLが空です");
            callback(null);
            yield break;
        }
        
        Debug.Log($"ギフトアイコンのダウンロードを開始します: {url}");
        
        // 直接URLから画像をダウンロード
        yield return StartCoroutine(TryDownloadImage(url, (sprite) => {
            if (sprite != null)
            {
                Debug.Log($"ギフトアイコンのダウンロードに成功しました: {url}");
                callback(sprite);
            }
            else
            {
                Debug.LogError($"ギフトアイコンのダウンロードに失敗しました: {url}");
                callback(null);
            }
        }));
    }
    
    // 画像ダウンロードを試みるコルーチン
    private IEnumerator TryDownloadImage(string url, System.Action<Sprite> callback)
    {
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            www.timeout = 10; // タイムアウトを10秒に設定
            
            // ユーザーエージェントを設定（一部のサーバーではこれが必要）
            www.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/58.0.3029.110 Safari/537.36");
            
            yield return www.SendWebRequest();
            
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                try
                {
                    Texture2D texture = ((UnityEngine.Networking.DownloadHandlerTexture)www.downloadHandler).texture;
                    if (texture != null)
                    {
                        // テクスチャのサイズが0の場合はエラー
                        if (texture.width <= 0 || texture.height <= 0)
                        {
                            Debug.LogError($"ダウンロードされたテクスチャのサイズが無効です: {texture.width}x{texture.height}, URL: {url}");
                            callback(null);
                            yield break;
                        }
                        
                        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                        Debug.Log($"アイコンをダウンロードしました: {url}, テクスチャサイズ: {texture.width}x{texture.height}");
                        
                        // スプライトのデバッグ情報
                        Debug.Log($"作成されたスプライト: {sprite != null}, 矩形: {sprite?.rect}, ピボット: {sprite?.pivot}");
                        
                        if (sprite != null)
                        {
                            callback(sprite);
                        }
                        else
                        {
                            Debug.LogError($"スプライトの作成に失敗しました: {url}");
                            callback(null);
                        }
                    }
                    else
                    {
                        Debug.LogError($"テクスチャの取得に失敗しました: {url}");
                        callback(null);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"テクスチャの処理中にエラーが発生しました: {e.Message}, URL: {url}");
                    callback(null);
                }
            }
            else
            {
                Debug.LogError($"アイコンのダウンロードに失敗しました: {www.error}, URL: {url}, レスポンスコード: {www.responseCode}");
                callback(null);
            }
        }
    }
    
    // コメントログをクリア
    public void ClearChatLog()
    {
        if (chatLogContainer == null) return;
        
        // 全てのアイテムを削除
        foreach (GameObject item in chatLogItems)
        {
            Destroy(item);
        }
        
        // リストをクリア
        chatLogItems.Clear();
        
        // スクロール位置をリセット
        if (chatScrollRect != null && chatScrollRect.content != null)
        {
            chatScrollRect.verticalNormalizedPosition = 1f; // 一番上に戻す
        }
    }
    
    // ギフトログをクリア
    public void ClearGiftLog()
    {
        if (giftLogContainer != null)
        {
            // 子オブジェクトをすべて削除
            foreach (Transform child in giftLogContainer)
            {
                Destroy(child.gameObject);
            }
            
            // リストをクリア
            giftLogItems.Clear();
            
            // ストリーク辞書もクリア
            giftStreaks.Clear();
            
            // スクロール位置をリセット
            if (giftScrollRect != null && giftScrollRect.content != null)
            {
                giftScrollRect.verticalNormalizedPosition = 1f; // 一番上に戻す
            }
            
            Debug.Log("ギフトログをクリアしました");
        }
    }

    private void OnDestroy()
    {
        // イベントハンドラを解除
        BgcTiktokWebSocket.OnConnectionError -= HandleConnectionError;
        BgcTiktokWebSocket.OnLikeReceived -= HandleLikeReceived;
        BgcTiktokWebSocket.OnChatReceived -= HandleChatReceived;
        BgcTiktokWebSocket.OnRoomUserReceived -= HandleRoomUserReceived;
        BgcTiktokWebSocket.OnGiftReceived -= HandleGiftReceived;
        BgcTiktokWebSocket.OnShareReceived -= HandleShareReceived;
        BgcTiktokWebSocket.OnFollowReceived -= HandleFollowReceived;
    }

    // シェアイベントを処理するメソッド
    private void HandleShareReceived(ShareMessage shareMessage)
    {
        if (shareMessage == null)
        {
            Debug.LogError("シェアメッセージがnullです");
            return;
        }

        Debug.Log($"シェアイベントを受信しました: {shareMessage.userId}, {shareMessage.nickname}");

        // シェアメッセージをチャットログに追加
        string shareText = $"{shareMessage.nickname}さんがライブをシェアしました！";
        AddChatLogItem(shareMessage.userId, shareMessage.nickname, shareMessage.uniqueId, shareText);

        // 必要に応じて追加の処理をここに実装
        // 例: シェアカウントの更新、特別なエフェクトの表示など
    }

    // フォローイベントを処理するメソッド
    private void HandleFollowReceived(FollowMessage followMessage)
    {
        if (followMessage == null)
        {
            Debug.LogError("フォローメッセージがnullです");
            return;
        }

        Debug.Log($"フォローイベントを受信しました: {followMessage.userId}, {followMessage.nickname}");

        // フォローメッセージをチャットログに追加
        string followText = $"{followMessage.nickname}さんがライブ配信者をフォローしました！";
        AddChatLogItem(followMessage.userId, followMessage.nickname, followMessage.uniqueId, followText);

        // 必要に応じて追加の処理をここに実装
        // 例: フォローカウントの更新、特別なエフェクトの表示など
    }

    // 接続状態を更新するコルーチン
    private IEnumerator UpdateConnectionStatusRoutine()
    {
        while (true)
        {
            UpdateConnectionStatus();
            yield return new WaitForSeconds(statusUpdateInterval);
        }
    }
}