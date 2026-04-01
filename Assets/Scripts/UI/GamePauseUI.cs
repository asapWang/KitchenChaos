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
        optionsButton.onClick.AddListener(() => OptionsUI.Instance.Show());
    }

    private void Start()
    {
        //订阅暂停游戏事件
        GameManager.Instance.OnPauseGame += GameManager_OnPauseGame;
        //订阅恢复游戏事件
        GameManager.Instance.OnResumeGame += GameManager_OnResumeGame;
        Hide();
    }
    private void GameManager_OnPauseGame(object sender, EventArgs e)
    {
        Show();
    }
    private void GameManager_OnResumeGame(object sender, EventArgs e)
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
    }
}
