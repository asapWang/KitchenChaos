using UnityEngine;
using UnityEngine.SceneManagement;

//场景加载器
public static class Loader
{
    //枚举场景类型，避免字符串的使用
    public enum Scene 
    {
        MainMenuScene,
        LoadingScene,
        GameScene,
    }
    //定义目标加载场景
    private static Scene targetScene;

    //保存目标场景并加载LoadingScene
    public static void LoadScene(Scene scene)
    {
        targetScene = scene;
        SceneManager.LoadScene(Scene.LoadingScene.ToString());
    }
    //回调函数，在LoadingScene场景的LoaderCallBack脚本中调用，正式加载目标场景
    public static void LoaderCallBack()
    {
        SceneManager.LoadScene(targetScene.ToString());
    }
}
