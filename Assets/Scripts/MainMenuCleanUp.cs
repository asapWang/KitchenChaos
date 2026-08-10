using UnityEngine;
using Unity.Netcode;

public class MainMenuCleanUp : MonoBehaviour
{
    //清理残留的NetworkManager和GameMultiplayer，避免在MainMenu场景中重复创建
    private void Awake()
    {
        if (GameMultiplayer.Instance != null)
        {
            Destroy(GameMultiplayer.Instance.gameObject);
        }
        if (NetworkManager.Singleton != null)
        {
            Destroy(NetworkManager.Singleton.gameObject);
        }
    }
}
