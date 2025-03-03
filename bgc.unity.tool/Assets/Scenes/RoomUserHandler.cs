using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using bgc.unity.tool;
using bgc.unity.tool.Models;
using System.Text;

public class RoomUserHandler : MonoBehaviour
{
    // 視聴者数を表示するテキスト
    [SerializeField] private Text viewerCountText;
    
    // トップ視聴者リストを表示するテキスト
    [SerializeField] private Text topViewersText;
    
    // 最大表示する視聴者数
    [SerializeField] private int maxTopViewers = 5;
    
    // 最後に更新した時間
    private float lastUpdateTime = 0f;
    
    // 更新間隔（秒）
    [SerializeField] private float updateInterval = 1f;
    
    // 最新の視聴者情報
    private RoomUserMessage latestRoomUserMessage;

    private void Start()
    {
        // イベントハンドラを登録
        BgcTiktokWebSocket.OnRoomUserReceived += HandleRoomUserReceived;
        
        // テキストを初期化
        if (viewerCountText != null)
        {
            viewerCountText.text = "視聴者数: 0";
        }
        
        if (topViewersText != null)
        {
            topViewersText.text = "トップ視聴者: なし";
        }
    }
    
    private void Update()
    {
        // 一定間隔で表示を更新
        if (Time.time - lastUpdateTime > updateInterval && latestRoomUserMessage != null)
        {
            UpdateUI(latestRoomUserMessage);
            lastUpdateTime = Time.time;
        }
    }
    
    // 部屋の視聴者情報を受信したときの処理
    private void HandleRoomUserReceived(RoomUserMessage roomUserMessage)
    {
        latestRoomUserMessage = roomUserMessage;
        UpdateUI(roomUserMessage);
    }
    
    // UI表示を更新
    private void UpdateUI(RoomUserMessage roomUserMessage)
    {
        // 視聴者数を更新
        if (viewerCountText != null)
        {
            viewerCountText.text = $"視聴者数: {roomUserMessage.viewerCount}人";
        }
        
        // トップ視聴者リストを更新
        if (topViewersText != null && roomUserMessage.topViewers != null && roomUserMessage.topViewers.Length > 0)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("トップ視聴者:");
            
            int count = Mathf.Min(maxTopViewers, roomUserMessage.topViewers.Length);
            for (int i = 0; i < count; i++)
            {
                TopViewer viewer = roomUserMessage.topViewers[i];
                if (viewer.user != null && !string.IsNullOrEmpty(viewer.user.nickname))
                {
                    sb.AppendLine($"{i+1}. {viewer.user.nickname} ({viewer.coinCount}コイン)");
                }
                else if (viewer.user != null && !string.IsNullOrEmpty(viewer.user.uniqueId))
                {
                    sb.AppendLine($"{i+1}. {viewer.user.uniqueId} ({viewer.coinCount}コイン)");
                }
                else
                {
                    sb.AppendLine($"{i+1}. 不明なユーザー ({viewer.coinCount}コイン)");
                }
            }
            
            topViewersText.text = sb.ToString();
        }
        else if (topViewersText != null)
        {
            topViewersText.text = "トップ視聴者: なし";
        }
    }
    
    private void OnDestroy()
    {
        // イベントハンドラを解除
        BgcTiktokWebSocket.OnRoomUserReceived -= HandleRoomUserReceived;
    }
} 