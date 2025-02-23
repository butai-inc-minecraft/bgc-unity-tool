using UnityEngine;
using WebSocketSharp;

public class hogehoge : MonoBehaviour
{
    // 固定の WebSocket 接続先 URL（例: サーバーが username をクエリパラメータとして受け取る場合）
    // ※必要に応じて変更してください
    private string baseUrl = "wss://tiktok-live-server-2.onrender.com/ws/";  

    private WebSocket ws;
    private bool isConnected = false;

    /// <summary>
    /// 外部から呼び出して接続を開始するメソッド。
    /// 引数の username を利用して接続先 URL にクエリパラメータとして付加します。
    /// </summary>
    /// <param name="username">ユーザー名</param>
    public void Connect(string username)
    {
        if (isConnected)
        {
            Debug.LogWarning("既に接続済みです。");
            return;
        }

        // 例として、username をクエリパラメータとして URL に付加
        string urlWithUser = $"{baseUrl}?username={username}";
        ws = new WebSocket(urlWithUser);

        // 各イベントハンドラーの登録
        ws.OnOpen += OnOpen;
        ws.OnMessage += OnMessage;
        ws.OnError += OnError;
        ws.OnClose += OnClose;

        // 非同期で接続開始
        ws.ConnectAsync();
    }

    private void OnOpen(object sender, System.EventArgs e)
    {
        isConnected = true;
        Debug.Log("WebSocket 接続が確立されました。");
        // 接続後に必要な初期処理があればここで実施
    }

    private void OnMessage(object sender, MessageEventArgs e)
    {
        Debug.Log("受信メッセージ: " + e.Data);
    }

    private void OnError(object sender, ErrorEventArgs e)
    {
        Debug.LogError("WebSocket エラー: " + e.Message);
    }

    private void OnClose(object sender, CloseEventArgs e)
    {
        isConnected = false;
        Debug.Log("WebSocket 接続が閉じられました。理由: " + e.Reason);
    }

    /// <summary>
    /// 外部から呼び出して接続を終了するメソッド
    /// </summary>
    public void Disconnect()
    {
        if (ws != null && isConnected)
        {
            ws.CloseAsync();
        }
        else
        {
            Debug.LogWarning("接続がないか、既に切断されています。");
        }
    }

    void OnDestroy()
    {
        if (ws != null)
        {
            ws.Close();
        }
    }
}