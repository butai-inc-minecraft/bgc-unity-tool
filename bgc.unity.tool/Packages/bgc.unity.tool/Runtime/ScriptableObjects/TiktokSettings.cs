using UnityEngine;

namespace bgc.unity.tool.ScriptableObjects
{
    [CreateAssetMenu(fileName = "TiktokSettings", menuName = "BGC/TikTok Settings", order = 1)]
    public class TiktokSettings : ScriptableObject
    {
        [Header("API設定")]
        [SerializeField, Tooltip("TikTok APIキー")]
        private string apiKey = "";

        [Header("接続設定")]
        [SerializeField, Tooltip("デフォルトのユーザー名")]
        private string defaultUsername = "default";

        [Header("デバッグ設定")]
        [SerializeField, Tooltip("詳細なログを出力するかどうか")]
        private bool verboseLogging = false;

        // APIキーのプロパティ
        public string ApiKey => apiKey;

        // デフォルトユーザー名のプロパティ
        public string DefaultUsername => defaultUsername;

        // 詳細ログフラグのプロパティ
        public bool VerboseLogging => verboseLogging;

        // シングルトンインスタンス
        private static TiktokSettings _instance;

        // インスタンスを取得するプロパティ
        public static TiktokSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Resources フォルダから設定を読み込む
                    _instance = Resources.Load<TiktokSettings>("TiktokSettings");

                    // 設定が見つからない場合は警告を表示
                    if (_instance == null)
                    {
                        Debug.LogWarning("TiktokSettings が見つかりません。Resources/TiktokSettings.asset を作成してください。");
                        // 一時的なインスタンスを作成
                        _instance = CreateInstance<TiktokSettings>();
                    }
                }
                return _instance;
            }
        }
    }
} 