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
        
        // ボタンがある場合は、リスナー登録を行う
        if (connectButton != null)
        {
            connectButton.onClick.AddListener(OnConnectButtonClicked);
        }

        // スクリプト開始時に自動で接続する
        // ※ InputField に初期値が設定されている必要があります
        if (usernameInputField != null && !string.IsNullOrEmpty(usernameInputField.text))
        {
            string username = usernameInputField.text;
            Debug.Log("自動接続: username = " + username);
            ConnectToWebSocket(username);
        }
    }
    
    private void Update()
    {
        // 接続状態を更新
        UpdateConnectionStatus();
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
        
        // チャットメッセージをログに表示
        Debug.Log($"💬 {nickname}: {comment}");
        
        // チャットメッセージをリストに追加
        AddChatMessage($"{nickname}: {comment}");
        
        // チャットメッセージのUI更新
        UpdateChatUI();
        
        // コメントログに追加
        AddChatLogItem(nickname, comment);
        
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
        
        // repeatEndがtrueの場合、ストリークを終了する
        if (repeatEnd)
        {
            string streakId = userId + "_" + giftId;
            if (giftStreaks.ContainsKey(streakId))
            {
                Debug.Log($"ストリーク終了: {streakId} - 辞書から削除します");
                giftStreaks.Remove(streakId);
            }
        }
        
        // ギフトログに追加または更新
        AddGiftLogItem(userId, nickname, giftName, giftId, diamondCount, repeatCount, repeatEnd, iconUrl);
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
    
    // エラーメッセージを表示する
    private void ShowErrorMessage(string message)
    {
        if (errorMessageText != null)
        {
            errorMessageText.text = "エラー: " + message;
            errorMessageText.color = Color.red;
        }
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
            connectionStatusText.text = "接続状態: " + (BgcTiktokWebSocket.IsConnected ? "接続中" : "未接続");
            connectionStatusText.color = BgcTiktokWebSocket.IsConnected ? Color.green : Color.gray;
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
    private void AddChatLogItem(string username, string comment)
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
        Debug.Log($"コメント追加: {username}, {comment}");
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
            chatItemComponent.SetChatInfo(username, comment);
            Debug.Log($"ChatItemPrefabコンポーネントを使用: {username}, {comment}");
            
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
            
            if (texts.Length >= 2)
            {
                texts[0].text = username + ":";
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
                texts[0].text = username + ": " + comment;
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
    private void AddGiftLogItem(string userId, string username, string giftName, int giftId, int diamonds, int repeatCount, bool repeatEnd, string giftIconUrl)
    {
        if (giftLogContainer == null)
        {
            Debug.LogError("ギフトログの親オブジェクトがアサインされていません。Inspector でアサインしてください。");
            return;
        }
        
        if (giftItemPrefab == null)
        {
            Debug.LogError("ギフトアイテムのプレハブがアサインされていません。Inspector でアサインしてください。");
            return;
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
            // ギフトアイコンがある場合は、画像をダウンロードして設定
            if (!string.IsNullOrEmpty(giftIconUrl))
            {
                StartCoroutine(DownloadGiftIcon(giftIconUrl, (sprite) => {
                    if (sprite != null)
                    {
                        Debug.Log($"GiftItemPrefabコンポーネントにギフトアイコンを設定します");
                        giftItemComponent.SetGiftInfo(username, giftName, diamonds, repeatCount, sprite, repeatEnd);
                        
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
                        Debug.LogError("ダウンロードされたスプライトがnullです。スプライトなしでSetGiftInfoを呼び出します");
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
                    Image giftIconImage = giftItem.GetComponentInChildren<Image>();
                    if (giftIconImage != null)
                    {
                        Debug.Log($"ギフトアイコン用のImageコンポーネントを見つけました: {giftIconImage.gameObject.name}");
                        
                        StartCoroutine(DownloadGiftIcon(giftIconUrl, (sprite) => {
                            if (sprite != null)
                            {
                                Debug.Log($"ギフトアイコンをImageに設定します: {giftIconImage.gameObject.name}");
                                giftIconImage.sprite = sprite;
                                giftIconImage.gameObject.SetActive(true);
                                
                                // レイアウトを更新
                                LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)giftIconImage.transform);
                                
                                // アイコンロード後にもスクロールを一番下に移動
                                if (giftScrollRect != null && giftScrollRect.content != null)
                                {
                                    StartCoroutine(ScrollToBottomNextFrame(giftScrollRect));
                                    Debug.Log("スクロールを一番下に移動しました");
                                }else{
                                    Debug.LogError("giftScrollRect.contentがnullです。ScrollRectのContentフィールドを設定してください。");
                                }
                            }
                            else
                            {
                                Debug.LogError("ダウンロードされたスプライトがnullです");
                            }
                        }));
                    }
                    else
                    {
                        // すべてのImageコンポーネントを検索
                        Image[] allImages = giftItem.GetComponentsInChildren<Image>(true);
                        Debug.Log($"プレハブ内のImageコンポーネント数: {allImages.Length}");
                        
                        if (allImages.Length > 0)
                        {
                            // 最初のImageコンポーネントを使用
                            Image firstImage = allImages[0];
                            Debug.Log($"最初のImageコンポーネントを使用します: {firstImage.gameObject.name}, アクティブ: {firstImage.gameObject.activeSelf}");
                            
                            StartCoroutine(DownloadGiftIcon(giftIconUrl, (sprite) => {
                                if (sprite != null)
                                {
                                    Debug.Log($"ギフトアイコンをImageに設定します: {firstImage.gameObject.name}");
                                    firstImage.sprite = sprite;
                                    firstImage.gameObject.SetActive(true);
                                    
                                    // レイアウトを更新
                                    LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)firstImage.transform);
                                    
                                    // アイコンロード後にもスクロールを一番下に移動
                                    if (giftScrollRect != null && giftScrollRect.content != null)
                                    {

                                        StartCoroutine(ScrollToBottomNextFrame(giftScrollRect));
                                        Debug.Log("スクロールを一番下に移動しました");
                                    }else{
                                        Debug.LogError("giftScrollRect.contentがnullです。ScrollRectのContentフィールドを設定してください。");  
                                    }
                                }
                                else
                                {
                                    Debug.LogError("ダウンロードされたスプライトがnullです");
                                }
                            }));
                        }
                        else
                        {
                            Debug.LogError("ギフトアイコン用のImageコンポーネントが見つかりません");
                        }
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
    }
    
    // ギフトアイコンをダウンロードするコルーチン
    private IEnumerator DownloadGiftIcon(string url, System.Action<Sprite> callback)
    {
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogWarning("ダウンロードURLが空です");
            callback(null);
            yield break;
        }

        Debug.Log($"アイコンのダウンロードを開始: {url}");
        
        // WebP形式かどうかをチェック
        bool isWebP = url.Contains(".webp") || url.EndsWith("format=webp") || url.Contains("format=webp");
        
        // TikTokのURLパターンを検出
        bool isTikTokUrl = url.Contains("tiktokcdn") || url.Contains("tiktok.com");
        
        // WebP形式またはTikTokのURLの場合は、代替フォーマットを試す
        if (isWebP || isTikTokUrl)
        {
            Debug.Log($"WebP形式またはTikTokのURLを検出しました: {url}");
            
            // URLにクエリパラメータを追加してJPEG形式を要求
            string jpegUrl = url;
            if (url.Contains("?"))
            {
                jpegUrl = url + "&format=jpeg";
            }
            else
            {
                jpegUrl = url + "?format=jpeg";
            }
            
            // WebP拡張子を持つ場合は置き換える
            jpegUrl = jpegUrl.Replace(".webp", ".jpeg");
            
            Debug.Log($"JPEG形式を試みます: {jpegUrl}");
            
            // まずJPEGを試す
            yield return StartCoroutine(TryDownloadImage(jpegUrl, (sprite) => {
                if (sprite != null)
                {
                    callback(sprite);
                }
                else
                {
                    // JPEGが失敗した場合、PNGを試す
                    string pngUrl = url.Replace(".webp", ".png").Replace("format=webp", "format=png");
                    Debug.Log($"JPEG形式が失敗しました。PNG形式を試みます: {pngUrl}");
                    
                    StartCoroutine(TryDownloadImage(pngUrl, callback));
                }
            }));
        }
        else
        {
            // WebPでない場合は通常通りダウンロード
            yield return StartCoroutine(TryDownloadImage(url, callback));
        }
    }
    
    // 画像ダウンロードを試みるコルーチン
    private IEnumerator TryDownloadImage(string url, System.Action<Sprite> callback)
    {
        using (UnityEngine.Networking.UnityWebRequest www = UnityEngine.Networking.UnityWebRequestTexture.GetTexture(url))
        {
            www.timeout = 10; // タイムアウトを10秒に設定
            
            yield return www.SendWebRequest();
            
            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                try
                {
                    Texture2D texture = ((UnityEngine.Networking.DownloadHandlerTexture)www.downloadHandler).texture;
                    if (texture != null)
                    {
                        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                        Debug.Log($"アイコンをダウンロードしました: {url}, テクスチャサイズ: {texture.width}x{texture.height}");
                        
                        // スプライトのデバッグ情報
                        Debug.Log($"作成されたスプライト: {sprite != null}, 矩形: {sprite?.rect}, ピボット: {sprite?.pivot}");
                        
                        callback(sprite);
                    }
                    else
                    {
                        Debug.LogError($"テクスチャの取得に失敗しました: {url}");
                        callback(null);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"テクスチャの処理中にエラーが発生しました: {e.Message}");
                    callback(null);
                }
            }
            else
            {
                Debug.LogError($"アイコンのダウンロードに失敗しました: {www.error}, URL: {url}");
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
        if (giftLogContainer == null) return;
        
        // 全てのアイテムを削除
        foreach (GameObject item in giftLogItems)
        {
            Destroy(item);
        }
        
        // リストをクリア
        giftLogItems.Clear();
        
        // ストリーク辞書をクリア
        giftStreaks.Clear();
        
        // スクロール位置をリセット
        if (giftScrollRect != null && giftScrollRect.content != null)
        {
            giftScrollRect.verticalNormalizedPosition = 1f; // 一番上に戻す
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
    }
}