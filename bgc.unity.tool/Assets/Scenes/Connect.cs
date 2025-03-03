using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using bgc.unity.tool;
using bgc.unity.tool.Services;
using bgc.unity.tool.Models;
using bgc.unity.tool.Utils;

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
    }

    private void Start()
    {
        // イベントハンドラを登録
        BgcTiktokWebSocket.OnConnectionError += HandleConnectionError;
        BgcTiktokWebSocket.OnLikeReceived += HandleLikeReceived;
        
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
        int totalLikeCount = likeMessage.totalLikeCount;
        
        // 前回の累積いいね数を算出
        int previousTotal = totalLikeCount - likeCount;
        
        // 今回の累積いいね数
        int currentTotal = totalLikeCount;
        
        // 前回と今回の累積いいね数の間に100の倍数があるかチェック
        CheckLikeThresholds(userId, nickname, previousTotal, currentTotal, 100);
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
                    Debug.Log($"🎉 {nickname}さんが100いいねごとを達成しました！ 🎉");
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
    
    private void OnDestroy()
    {
        // イベントハンドラを解除
        BgcTiktokWebSocket.OnConnectionError -= HandleConnectionError;
        BgcTiktokWebSocket.OnLikeReceived -= HandleLikeReceived;
    }
}