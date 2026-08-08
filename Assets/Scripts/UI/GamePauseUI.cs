using System;
using UnityEngine;
using UnityEngine.UI;

public class GamePauseUI : MonoBehaviour
{
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button optionsButton;
    private void Awake()
    {
        resumeButton.onClick.AddListener(() => GameManager.Instance.TogglePauseGame());
        mainMenuButton.onClick.AddListener(() => Loader.LoadScene(Loader.Scene.MainMenuScene));
        //点击options按钮时，打开options面板，并传入一个事件：当options面板关闭时要调用的事件（这里是重新打开暂停面板）
        optionsButton.onClick.AddListener(() => {
            Hide();
            OptionsUI.Instance.Show(Show);
        });
    }

    private void Start()
    {
        //订阅暂停游戏事件
        GameManager.Instance.OnLocalPauseGame += GameManager_OnLocalPauseGame;
        //订阅恢复游戏事件
        GameManager.Instance.OnLocalUnpauseGame += GameManager_OnLocalUnpauseGame;
        
        Hide();
    }
    private void GameManager_OnLocalPauseGame(object sender, EventArgs e)
    {
        Show();
    }
    private void GameManager_OnLocalUnpauseGame(object sender, EventArgs e)
    {
        Hide();
    }
    private void Hide()
    {
        gameObject.SetActive(false);
    }
    private void Show()
    {
        gameObject.SetActive(true);
        //高亮resume按钮，方便玩家手柄和键盘操作
        resumeButton.Select();
    }
}
