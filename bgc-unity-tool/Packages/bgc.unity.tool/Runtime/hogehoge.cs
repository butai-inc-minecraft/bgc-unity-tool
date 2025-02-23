using UnityEngine;
using WebSocketSharp;

namespace bgc.unity.tool
{
    public class hogehoge : MonoBehaviour
    {
        // 固定の WebSocket 接続先 URL
        private string baseUrl = "wss://tiktok-live-server-2.onrender.com/ws/";  

        private WebSocket ws;
        private bool isConnected = false;

        public void Connect(string username)
        {
            if (isConnected)
            {
                Debug.LogWarning("既に接続済みです。");
                return;
            }
            string urlWithUser = $"{baseUrl}?username={username}";
            ws = new WebSocket(urlWithUser);

            ws.OnOpen += OnOpen;
            ws.OnMessage += OnMessage;
            ws.OnError += OnError;
            ws.OnClose += OnClose;

            ws.ConnectAsync();
        }

        private void OnOpen(object sender, System.EventArgs e)
        {
            isConnected = true;
            Debug.Log("WebSocket 接続が確立されました。");
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
}