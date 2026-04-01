using UnityEngine;

public class DataMgr
{
    //new一个DataMgr实例
    private static DataMgr instance = new DataMgr();
    public static DataMgr Instance => instance;
    //创建临时的SettingsData对象
    private SettingsData settingsData;
    public SettingsData SettingsData=> settingsData;

    //私有构造函数，防止外部实例化
    private DataMgr()
    {
        //加载SettingsData数据
        settingsData = JsonMgr.Instance.LoadData<SettingsData>("SettingsData");

    }

    //保存SettingsData数据
    public void SaveSettingsData()
    {
        JsonMgr.Instance.SaveData(settingsData, "SettingsData");
    }
}
