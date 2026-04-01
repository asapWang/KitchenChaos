using System.IO;
using UnityEngine;
using LitJson;

public class JsonMgr
{
    public enum JsonType
    {
        jsonUtility,
        litJson,
    }
    //单例模式
    private static JsonMgr instance=new JsonMgr();
    public static JsonMgr Instance=>instance;
    private JsonMgr(){}
    //序列化
    public void SaveData(object data,string fileName,JsonType type=JsonType.litJson)
    {
        string path = Application.persistentDataPath + "/" + fileName;
        string jsonStr = "";
        switch (type)
        {
            case JsonType.jsonUtility:
                jsonStr = JsonUtility.ToJson(data);
                break;
            case JsonType.litJson:
                jsonStr = JsonMapper.ToJson(data);
                break;
        }
        File.WriteAllText(path, jsonStr);
    }
    //反序列化
    public T LoadData<T>(string fileName,JsonType type=JsonType.litJson) where T: new()
    {
        string path = Application.streamingAssetsPath + "/" + fileName;
        //判断文件是否存在，如果不存在则从持久化路径中读取
        if (!File.Exists(path))
        {
            path = Application.persistentDataPath + "/" + fileName;
            if(!File.Exists(path))
                return new T();
        }
        T data = default;
        string jsonStr = File.ReadAllText(path);
        switch (type)
        {
            case JsonType.jsonUtility:
                data=JsonUtility.FromJson<T>(jsonStr);
                break;
            case JsonType.litJson:
                data=JsonMapper.ToObject<T>(jsonStr);
                break;
        }
        return data;
    }
}
