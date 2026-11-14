using UnityEngine;
using UnityEngine.UI;

public class GamePauseUI : BasePanel
{
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button optionsButton;
    private void Awake()
    {
        resumeButton.onClick.AddListener(() => GameManager.Instance.TogglePauseGame());
        mainMenuButton.onClick.AddListener(() => {
            GameMultiplayer.Instance.ShutdownNetwork();
            Loader.LoadScene(Loader.Scene.MainMenuScene);
        });
        UIManager uiManager = GetComponentInParent<UIManager>();
        optionsButton.onClick.AddListener(() => uiManager.PushPanel(nameof(OptionsUI)));
    }

    public override void Enter()
    {
        //高亮resume按钮，方便玩家手柄和键盘操作
        resumeButton.Select();
    }

    public override void Resume()
    {
        Enter();
    }
}
