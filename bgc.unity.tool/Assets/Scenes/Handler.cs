using UnityEngine;
using bgc.unity.tool;

public class Handler : MonoBehaviour
{
    private hogehoge websocketHandler;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitializeOnLoad()
    {
        Debug.Log("hello");
        // Handler コンポーネント付きの GameObject を作成し、シーン遷移時にも破棄されないようにする
        GameObject go = new GameObject("Handler");
        go.AddComponent<Handler>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        websocketHandler = new hogehoge();
        ConnectToWebSocket("defaultUser"); // "defaultUser" は接続に使用するデフォルトのユーザー名です
    }

    public void ConnectToWebSocket(string username)
    {
        websocketHandler.Connect(username);
    }

    public void DisconnectFromWebSocket()
    {
        websocketHandler.Disconnect();
    }

    private void OnDestroy()
    {
        websocketHandler.Disconnect();
    }
}