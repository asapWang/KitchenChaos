using UnityEngine;
using Unity.Netcode;
using UnityEngine.UI;
public class HostDisconnectUI : MonoBehaviour
{
    [SerializeField] private Button mainMenuButton;
    private void Awake()
    {
        mainMenuButton.onClick.AddListener(() => Loader.LoadScene(Loader.Scene.MainMenuScene));
    }
    private void Start()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
        Hide();
    }
    private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
    {
        if (!NetworkManager.Singleton.IsServer &&
        !NetworkManager.Singleton.ShutdownInProgress &&
        clientId == NetworkManager.Singleton.LocalClientId)
        {
            Show();
        }
    }
    private void Show()
    {
        gameObject.SetActive(true);
    }
    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
