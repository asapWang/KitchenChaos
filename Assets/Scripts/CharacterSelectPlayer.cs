using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

public class CharacterSelectPlayer : MonoBehaviour
{
    [SerializeField] private int playerIndex;
    [SerializeField] private GameObject readyText;
    [SerializeField] private PlayerSurface playerSurface;
    [SerializeField] private Button kickButton;
    [SerializeField] private TMP_Text playerNameText;
    private void Awake()
    {
        kickButton.onClick.AddListener(() =>
        {
            //服务端踢掉玩家
            PlayerData playerData = GameMultiplayer.Instance.GetPlayerDataFromIndex(playerIndex);
            GameMultiplayer.Instance.KickPlayer(playerData.clientId);
            GameLobby.Instance.KickPlayer(playerData.playerId.ToString());
        });
    }
    private void Start()
    {
        GameMultiplayer.Instance.OnPlayerDataNetworkListChanged += GameMultiplayer_OnPlayerDataNetworkListChanged;
        CharacterSelectReady.Instance.OnReadyChanged += CharacterSelectReady_OnReadyChanged;
        kickButton.gameObject.SetActive(NetworkManager.Singleton.IsServer);
        UpdatePlayer();
    }
    private void GameMultiplayer_OnPlayerDataNetworkListChanged(object sender, System.EventArgs e)
    {
        UpdatePlayer();
    }
    private void CharacterSelectReady_OnReadyChanged(object sender, System.EventArgs e)
    {
        UpdatePlayer();
    }
    //更新要显示的Player
    private void UpdatePlayer()
    {
        if(GameMultiplayer.Instance.IsPlayerIndexConnected(playerIndex))
        {
            Show();
            //每个客户端上的每个CharacterSelectPlayer都检测自己对应的玩家是否准备好
            //每个CharacterSelectPlayer的Index对应GameMultiplayer.Instance.playerDataNetworkList的Index
            PlayerData playerData = GameMultiplayer.Instance.GetPlayerDataFromIndex(playerIndex);
            //根据玩家准备状态更新UI
            readyText.SetActive(CharacterSelectReady.Instance.IsPlayerReady(playerData.clientId));
            //根据颜色ID设置玩家颜色
            playerSurface.SetPlayerColor(GameMultiplayer.Instance.GetPlayerColor(playerData.colorID));
            //根据玩家名字更新UI
            playerNameText.text = playerData.playerName.ToString();
        }
        else
        {
            Hide();
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
    private void OnDestroy()
    {
        if(GameMultiplayer.Instance != null)
        {
            GameMultiplayer.Instance.OnPlayerDataNetworkListChanged -= GameMultiplayer_OnPlayerDataNetworkListChanged;
        }
    }
}
