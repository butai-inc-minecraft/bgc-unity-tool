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
    
    // 表示する最大チャットメッセージ数
    [SerializeField] private int maxChatMessages = 5;
    
    // チャットメッセージのリスト
    private List<string> recentChatMessages = new List<string>();
    
    // ユーザーごとの累積いいね数を記録する辞書
    private Dictionary<string, int> userTotalLikes = new Dictionary<string, int>();

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
    }

    private void Start()
    {
        // イベントハンドラを登録
        BgcTiktokWebSocket.OnConnectionError += HandleConnectionError;
        BgcTiktokWebSocket.OnLikeReceived += HandleLikeReceived;
        BgcTiktokWebSocket.OnChatReceived += HandleChatReceived;
        
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
        
        // username を設定して接続開始
        TiktokWebSocketService.SetUsername(username);
        TiktokWebSocketService.Connect();
        Debug.Log("接続を開始しました。");
    }

    // 必要に応じて切断処理を呼び出す
    private void DisconnectFromWebSocket()
    {
        TiktokWebSocketService.Disconnect();
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
        
        // 前回の累積いいね数を取得（存在しない場合は0）
        int previousTotal = 0;
        if (userTotalLikes.ContainsKey(userId))
        {
            previousTotal = userTotalLikes[userId];
        }
        
        // 今回の累積いいね数を計算
        int currentTotal = previousTotal + likeCount;
        
        // 前回と今回の累積いいね数の間に100の倍数があるかチェック
        CheckLikeThresholds(userId, nickname, previousTotal, currentTotal, 100);
        
        // 累積いいね数を更新
        userTotalLikes[userId] = currentTotal;
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
    
    // いいねの閾値チェックを行うメソッド
    private void CheckLikeThresholds(string userId, string nickname, int previousTotal, int currentTotal, int threshold)
    {
        // 前回の閾値を超えた回数（100で割った商）
        int previousThresholdCount = previousTotal / threshold;
        
        // 今回の閾値を超えた回数（100で割った商）
        int currentThresholdCount = currentTotal / threshold;
        
        // 閾値を超えた回数が増えた場合
        if (currentThresholdCount > previousThresholdCount)
        {
            // 前回と今回の間にある閾値の倍数をすべて処理
            for (int i = previousThresholdCount + 1; i <= currentThresholdCount; i++)
            {
                int achievedCount = i * threshold;
                
                // 100いいねごとに異なるメッセージを表示
                if (achievedCount == 100)
                {
                    Debug.Log($"🎉 {nickname}さんが100いいねを達成しました！ 🎉");
                }
                else if (achievedCount == 200)
                {
                    Debug.Log($"🎊 {nickname}さんが200いいねを達成しました！すごい！ 🎊");
                }
                else if (achievedCount == 500)
                {
                    Debug.Log($"💯 {nickname}さんが500いいねを達成しました！素晴らしい！ 💯");
                }
                else if (achievedCount == 1000)
                {
                    Debug.Log($"🏆 {nickname}さんが1000いいねを達成しました！伝説級！ 🏆");
                }
                else
                {
                    Debug.Log($"👍 {nickname}さんが{achievedCount}いいねを達成しました！ 👍");
                }
            }
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
    
    private void OnDestroy()
    {
        // イベントハンドラを解除
        BgcTiktokWebSocket.OnConnectionError -= HandleConnectionError;
        BgcTiktokWebSocket.OnLikeReceived -= HandleLikeReceived;
        BgcTiktokWebSocket.OnChatReceived -= HandleChatReceived;
    }
}