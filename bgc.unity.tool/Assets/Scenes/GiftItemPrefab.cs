using UnityEngine;
using UnityEngine.UI;
using System.Collections;

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
        [SerializeField] private Text repeatCountText; // リピートカウント表示用のテキスト
        
        // リピート終了フラグ
        private bool isRepeatEnded = false;
        
        // ユーザーIDとギフトIDを保存
        private string userId = "";
        private int giftId = 0;
        
        // リピート終了フラグを外部から取得するためのプロパティ
        public bool IsRepeatEnded => isRepeatEnded;
        
        // ストリークIDを取得するメソッド
        public string GetStreakId()
        {
            return userId + "_" + giftId;
        }
        
        // ギフトアイコンのImageコンポーネントを取得するメソッド
        public Image GetGiftIcon()
        {
            return giftIcon;
        }
        
        /// <summary>
        /// ギフト情報を設定
        /// </summary>
        /// <param name="username">ユーザー名</param>
        /// <param name="giftName">ギフト名</param>
        /// <param name="diamonds">ダイヤモンド数</param>
        /// <param name="repeatCount">繰り返し回数</param>
        /// <param name="giftIconSprite">ギフトアイコン（オプション）</param>
        /// <param name="repeatEnded">リピート終了フラグ（オプション）</param>
        public void SetGiftInfo(string username, string giftName, int diamonds, int repeatCount, Sprite giftIconSprite = null, bool repeatEnded = false)
        {
            // リピート終了フラグを設定
            isRepeatEnded = repeatEnded;
            
            // ユーザーIDとギフトIDが設定されているか確認
            if (string.IsNullOrEmpty(userId))
            {
                Debug.LogWarning($"ユーザーIDが設定されていません。ユーザー名: {username}, ギフト名: {giftName}");
            }
            
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
                                   "Cost: " + diamonds + " Diamonds";
            }
            
            // リピートカウントの表示
            if (repeatCountText != null)
            {
                // リピート回数の表示
                repeatCountText.text = "x" + repeatCount;
                
                if (isRepeatEnded)
                {
                    // リピート終了時は緑色で表示（repeatCountに関わらず）
                    repeatCountText.color = new Color(0.0f, 0.8f, 0.0f); // 緑色
                    repeatCountText.fontSize = Mathf.Max(repeatCountText.fontSize, 16); // 最低でも16ポイント
                    
                    // リピート終了時は点滅させない
                }
                else
                {
                    // リピート中は赤色で表示（repeatCountに関わらず）
                    repeatCountText.color = Color.red;
                    repeatCountText.fontSize = Mathf.Max(repeatCountText.fontSize, 16); // 最低でも16ポイント
                    
                    // リピート中は点滅させる
                    StartCoroutine(BlinkText(repeatCountText));
                }
                
                // リピートカウントテキストを表示
                repeatCountText.gameObject.SetActive(true);
            }
            else if (giftInfoText != null)
            {
                // repeatCountTextがない場合は従来通りgiftInfoTextに含める
                string repeatText;
                
                if (isRepeatEnded)
                {
                    // リピート終了時は緑色で表示
                    repeatText = "<color=#00CC00>x" + repeatCount + "</color>";
                }
                else
                {
                    // リピート中は赤色で表示
                    repeatText = "<color=red>x" + repeatCount + "</color>";
                }
                
                giftInfoText.text = "Name: " + giftName + " (ID:" + diamonds + ")\n" +
                                   "Repeat: " + repeatText + "\n" +
                                   "Cost: " + diamonds + " Diamonds";
            }
            
            if (giftIcon != null && giftIconSprite != null)
            {
                Debug.Log($"ギフトアイコンを設定します: {giftIconSprite != null}, サイズ: {giftIconSprite?.rect.width}x{giftIconSprite?.rect.height}");
                giftIcon.sprite = giftIconSprite;
                giftIcon.gameObject.SetActive(true);
                
                // 画像が表示されるように設定を調整
                giftIcon.preserveAspect = true;
            }
            else if (giftIcon != null)
            {
                // アイコンが指定されていない場合はデフォルトアイコンを使用するか非表示にする
                Debug.LogWarning("ギフトアイコンが指定されていないため、非表示にします");
                giftIcon.gameObject.SetActive(false);
            }
            else
            {
                Debug.LogError("giftIconがnullです。Inspectorで設定してください。");
            }
        }
        
        // テキストを点滅させるコルーチン
        private IEnumerator BlinkText(Text text)
        {
            // 既に点滅中なら終了
            if (text.GetComponent<MonoBehaviour>().IsInvoking("BlinkText"))
                yield break;
                
            Color originalColor = text.color;
            Color blinkColor = new Color(1f, 0.3f, 0.3f); // 明るい赤色
            
            // リピート終了していたら点滅させない
            if (isRepeatEnded)
                yield break;
                
            // 5回点滅させる
            for (int i = 0; i < 5; i++)
            {
                // リピート終了していたら点滅を中止
                if (isRepeatEnded)
                    break;
                    
                text.color = blinkColor;
                yield return new WaitForSeconds(0.3f);
                text.color = originalColor;
                yield return new WaitForSeconds(0.3f);
            }
        }
        
        // ユーザーIDとギフトIDを設定するメソッド
        public void SetUserAndGiftId(string userId, int giftId)
        {
            this.userId = userId;
            this.giftId = giftId;
        }
    }
} 