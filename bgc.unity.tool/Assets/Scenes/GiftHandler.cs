using UnityEngine;
using bgc.unity.tool;
using bgc.unity.tool.Models;

public class GiftHandler : MonoBehaviour
{
       void OnEnable()
    {
    BgcTiktokWebSocket.OnGiftReceived += HandleGift;
    }

    void OnDisable()
    {
        BgcTiktokWebSocket.OnGiftReceived -= HandleGift;
    }

        // ギフト受信時に呼ばれる関数
    void HandleGift(GiftMessage giftMessage)
    {
        // ギフトが購入されるごとに
        Debug.Log($"ギフト受信: 送信者={giftMessage.profileName} ギフト名={giftMessage.giftName} ギフトID={giftMessage.giftId} コイン数={giftMessage.diamondCount} 個数={giftMessage.combo}");

        // ここに、ギフトやコインごとの処理を追加

        // 例：1コインギフトが購入された時
        if (giftMessage.diamondCount == 1)
        {
            Debug.Log("1コインギフトが購入されました。");
        }
        // バラギフトが購入された時
        else if (giftMessage.giftName == "Rose")
        {
            Debug.Log("バラギフトが購入されました。");
        }
        else{
            Debug.Log("非対応のギフトです");
        }
    }
}