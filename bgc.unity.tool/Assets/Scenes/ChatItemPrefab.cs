using UnityEngine;
using UnityEngine.UI;

namespace bgc.unity.tool
{
    /// <summary>
    /// コメントアイテムのUIコンポーネント
    /// </summary>
    public class ChatItemPrefab : MonoBehaviour
    {
        [SerializeField] private Text usernameText;
        [SerializeField] private Text commentText;
        [SerializeField] private Image userIcon;
        
        /// <summary>
        /// コメント情報を設定
        /// </summary>
        /// <param name="username">ユーザー名</param>
        /// <param name="comment">コメント内容</param>
        /// <param name="iconSprite">ユーザーアイコン（オプション）</param>
        public void SetChatInfo(string username, string comment, Sprite iconSprite = null)
        {
            if (usernameText != null)
            {
                usernameText.text = username + ":";
            }
            
            if (commentText != null)
            {
                commentText.text = comment;
            }
            
            if (userIcon != null && iconSprite != null)
            {
                userIcon.sprite = iconSprite;
                userIcon.gameObject.SetActive(true);
            }
            else if (userIcon != null)
            {
                // アイコンが指定されていない場合はデフォルトアイコンを使用するか非表示にする
                userIcon.gameObject.SetActive(false);
            }
        }
    }
} 