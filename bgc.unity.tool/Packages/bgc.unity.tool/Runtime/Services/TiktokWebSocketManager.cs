using UnityEngine;

namespace bgc.unity.tool.Services
{
    /// <summary>
    /// TiktokWebSocketServiceのメッセージキューをメインスレッドで処理するためのコンポーネント
    /// </summary>
    public class TiktokWebSocketManager : MonoBehaviour
    {
        private static TiktokWebSocketManager _instance;
        
        public static TiktokWebSocketManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // シーン内にインスタンスがなければ作成
                    GameObject go = new GameObject("TiktokWebSocketManager");
                    _instance = go.AddComponent<TiktokWebSocketManager>();
                    DontDestroyOnLoad(go); // シーン遷移時も破棄されないように
                }
                return _instance;
            }
        }
        
        private void Awake()
        {
            // 重複インスタンスのチェック
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        
        private void Update()
        {
            // WebSocketのメッセージキューを処理
            TiktokWebSocketService.ProcessMessageQueue();
        }
        
        /// <summary>
        /// TikTok WebSocketサーバーに接続します
        /// </summary>
        public void Connect()
        {
            TiktokWebSocketService.Connect();
        }
        
        /// <summary>
        /// TikTok WebSocketサーバーから切断します
        /// </summary>
        public void Disconnect()
        {
            TiktokWebSocketService.Disconnect();
        }
        
        /// <summary>
        /// ユーザー名を設定します
        /// </summary>
        /// <param name="username">設定するユーザー名</param>
        public void SetUsername(string username)
        {
            TiktokWebSocketService.SetUsername(username);
        }
        
        /// <summary>
        /// 接続状態を取得します
        /// </summary>
        public bool IsConnected => TiktokWebSocketService.IsConnected;
        
        private void OnDestroy()
        {
            // コンポーネントが破棄されるときに接続を切断
            TiktokWebSocketService.Disconnect();
        }
        
        private void OnApplicationQuit()
        {
            // アプリケーション終了時にクリーンアップ
            TiktokWebSocketService.Cleanup();
        }
    }
} 