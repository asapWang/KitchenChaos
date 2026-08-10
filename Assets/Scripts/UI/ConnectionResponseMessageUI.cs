using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class ConnectionResponseMessageUI : MonoBehaviour
{
    [SerializeField] private Button backButton;
    [SerializeField] private TextMeshProUGUI messageText;
    private void Awake()
    {
        backButton.onClick.AddListener(() =>
        {
            Hide();
        });
    }
    private void Start()
    {
        GameMultiplayer.Instance.OnFailedToJoinGame += GameMultiplayer_OnFailedToJoinGame;
        Hide();
    }
    private void GameMultiplayer_OnFailedToJoinGame(object sender, System.EventArgs e)
    {
        Show();
        messageText.text = NetworkManager.Singleton.DisconnectReason.ToString();
        if(NetworkManager.Singleton.DisconnectReason == null)
        {
            messageText.text = "Failed to connect.";
        }
    }
    private void Show()
    {
        this.gameObject.SetActive(true);
    }
    private void Hide()
    {
        this.gameObject.SetActive(false);
    }
    //此UI与networkManager的生命周期不同，所以在OnDestroy中取消订阅事件
    private void OnDestroy()
    {
        GameMultiplayer.Instance.OnFailedToJoinGame -= GameMultiplayer_OnFailedToJoinGame;
    }
}
