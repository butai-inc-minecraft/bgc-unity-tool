using UnityEngine;
using UnityEngine.UI;

namespace bgc.unity.tool
{
    /// <summary>
    /// コメントアイテムのUIコンポーネント
    /// </summary>
    public class ChatItemPrefab : MonoBehaviour
    {
        [SerializeField] private Text nicknameText;
        [SerializeField] private Text uniqueIdText;
        [SerializeField] private Text commentText;
    
        
        /// <summary>
        /// コメント情報を設定
        /// </summary>
        /// <param name="uniqueId">ユーザーID</param>
        /// <param name="nickname">ニックネーム</param>
        /// <param name="comment">コメント内容</param>
        public void SetChatInfo(string uniqueId, string nickname, string comment)
        {
            if (nicknameText != null)
            {
                nicknameText.text = nickname;
            }
            
            if (uniqueIdText != null)
            {
                uniqueIdText.text = "@" + uniqueId;
            }
            
            if (commentText != null)
            {
                commentText.text = comment;
            }
        }
    }
} 