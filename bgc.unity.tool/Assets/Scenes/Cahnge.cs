using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Cahnge : MonoBehaviour
{
    [SerializeField] private Button changeSceneButton;
    [SerializeField] private string scene1Name = "Scene1";
    [SerializeField] private string scene2Name = "Scene2";
    
    private bool isScene1Active = true;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (changeSceneButton != null)
        {
            changeSceneButton.onClick.AddListener(ToggleScene);
        }
        else
        {
            Debug.LogError("ボタンがアタッチされていません。インスペクターでボタンを設定してください。");
        }
    }

    // シーンを交互に切り替えるメソッド
    public void ToggleScene()
    {
        if (isScene1Active)
        {
            Debug.Log(scene2Name + "に切り替えます");
            SceneManager.LoadScene(scene2Name);
        }
        else
        {
            Debug.Log(scene1Name + "に切り替えます");
            SceneManager.LoadScene(scene1Name);
        }
        
        isScene1Active = !isScene1Active;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
