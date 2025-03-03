using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using bgc.unity.tool;

public class Handler : MonoBehaviour
{
    // 同一 GameObject にアタッチされた BgcTiktokWebSocket コンポーネントを取得
    private BgcTiktokWebSocket bgcTiktokWebSocket;
    
    // UI の InputField（インスペクターでアサイン）
    [SerializeField] private InputField usernameInputField;
    
    // オプション：ボタンを使う場合はインスペクターでアサイン
    [SerializeField] private Button connectButton;

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
    }

    private void Start()
    {
        // ボタンがある場合は、リスナー登録を行う
        if (connectButton != null)
        {
            connectButton.onClick.AddListener(OnConnectButtonClicked);
        }

        // スクリプト開始時に自動で接続する
        // ※ InputField に初期値が設定されている必要があります
        if (usernameInputField != null)
        {
            string username = usernameInputField.text;
            Debug.Log("自動接続: username = " + username);
            ConnectToWebSocket(username);
        }
        else
        {
            Debug.LogError("usernameInputField がアサインされていません。");
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
        }
    }

    // 指定された username で接続を試みる
    private void ConnectToWebSocket(string username)
    {
        if (string.IsNullOrEmpty(username))
        {
            Debug.LogWarning("Username が空です。接続できません。");
            return;
        }
        
        if (bgcTiktokWebSocket != null)
        {
            // username を設定して接続開始
            BgcTiktokWebSocket.SetUsername(username);
            bgcTiktokWebSocket.Connect();
            Debug.Log("Connect ボタンが押されました！");
        }
        else
        {
            Debug.LogError("BgcTiktokWebSocket が初期化されていません。\nUnityEngine.Debug:LogError (object)\nHandler:ConnectToWebSocket (string) (at Assets/Scenes/Connect.cs:84)\nHandler:OnConnectButtonClicked () (at Assets/Scenes/Connect.cs:58)\nUnityEngine.EventSystems.EventSystem:Update () (at ./Library/PackageCache/com.unity.ugui@03407c6d8751/Runtime/UGUI/EventSystem/EventSystem.cs:530)");
        }
    }

    // 必要に応じて切断処理を呼び出す
    private void DisconnectFromWebSocket()
    {
        if (bgcTiktokWebSocket != null)
        {
            bgcTiktokWebSocket.Disconnect();
        }
        else
        {
            Debug.LogError("BgcTiktokWebSocket が初期化されていません。");
        }
    }
}