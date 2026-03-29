using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;
    private void Awake()
    {
        startButton.onClick.AddListener(() =>
        {
            //加载游戏场景
            Loader.LoadScene(Loader.Scene.GameScene);
        });
        
        quitButton.onClick.AddListener(() => 
        {
            //退出游戏
            Application.Quit();
        });
    }

}
