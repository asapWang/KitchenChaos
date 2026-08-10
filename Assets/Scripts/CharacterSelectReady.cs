using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
public class CharacterSelectReady : NetworkBehaviour
{
    public static CharacterSelectReady Instance { get; private set; }
    private Dictionary <ulong, bool> playerReadyDictionary; 
    private void Awake()
    {
        playerReadyDictionary = new Dictionary<ulong, bool>();
        Instance = this;
    }
    public void SetPlayerReady()
    {
        SetPlayerReadyServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerReadyServerRpc(ServerRpcParams serverRpcParams = default)
    {
        //不让客户端传递ownerClientId，防止作弊，所以使用ServerRpcParams获取调用ServerRpc的客户端ID
        //ServerRpcParams参数包含了调用ServerRpc的客户端信息，包括客户端ID、网络连接等。通过这个参数，服务器可以知道是哪个客户端调用了这个ServerRpc，从而进行相应的处理。
        playerReadyDictionary[serverRpcParams.Receive.SenderClientId] = true;
        //检查所有玩家是否准备好
        bool allPlayersReady = true;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (!playerReadyDictionary.ContainsKey(clientId) || !playerReadyDictionary[clientId])
            {
                allPlayersReady = false;
                break;
            }
        }
        Debug.Log(allPlayersReady);
        if (allPlayersReady)
        {
            Loader.LoadNetwork(Loader.Scene.GameScene);
        }
    }
}
