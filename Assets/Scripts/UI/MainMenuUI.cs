using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button multiplayerButton;
    [SerializeField] private Button singlePlayerButton;
    [SerializeField] private Button quitButton;
    private void Awake()
    {
        multiplayerButton.onClick.AddListener(() =>
        {
            GameMultiplayer.IsMultiplayer = true;
            //加载大厅场景
            Loader.LoadScene(Loader.Scene.LobbyScene);
        });
        singlePlayerButton.onClick.AddListener(() =>
        {
            GameMultiplayer.IsMultiplayer = false;
            //加载大厅场景
            Loader.LoadScene(Loader.Scene.LobbyScene);
        });
        
        quitButton.onClick.AddListener(() => 
        {
            //退出游戏
            Application.Quit();
        });
        //设置时间缩放为1，确保游戏正常运行
        Time.timeScale = 1;
    }

}
