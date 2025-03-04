using UnityEngine;
using UnityEngine.UI;

namespace bgc.unity.tool
{
    /// <summary>
    /// ギフトアイテムのUIコンポーネント
    /// </summary>
    public class GiftItemPrefab : MonoBehaviour
    {
        [SerializeField] private Text usernameText;
        [SerializeField] private Text giftNameText;
        [SerializeField] private Text giftInfoText;
        [SerializeField] private Image giftIcon;
        
        /// <summary>
        /// ギフト情報を設定
        /// </summary>
        /// <param name="username">ユーザー名</param>
        /// <param name="giftName">ギフト名</param>
        /// <param name="diamonds">ダイヤモンド数</param>
        /// <param name="repeatCount">繰り返し回数</param>
        /// <param name="giftIconSprite">ギフトアイコン（オプション）</param>
        public void SetGiftInfo(string username, string giftName, int diamonds, int repeatCount, Sprite giftIconSprite = null)
        {
            if (usernameText != null)
            {
                usernameText.text = username + ":";
            }
            
            if (giftNameText != null)
            {
                giftNameText.text = "Sent " + giftName;
            }
            
            if (giftInfoText != null)
            {
                giftInfoText.text = "Name: " + giftName + " (ID:" + diamonds + ")\n" +
                                   "Repeat: x" + repeatCount + "\n" +
                                   "Cost: " + diamonds + " Diamonds";
            }
            
            if (giftIcon != null && giftIconSprite != null)
            {
                giftIcon.sprite = giftIconSprite;
                giftIcon.gameObject.SetActive(true);
            }
            else if (giftIcon != null)
            {
                // アイコンが指定されていない場合はデフォルトアイコンを使用するか非表示にする
                giftIcon.gameObject.SetActive(false);
            }
        }
    }
} 