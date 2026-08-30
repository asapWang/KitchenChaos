using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using System;
public class CharacterSelectReady : NetworkBehaviour
{
    public static CharacterSelectReady Instance { get; private set; }
    private Dictionary <ulong, bool> playerReadyDictionary; 
    //每个客户端知道其他客户端准备状态改变的事件
    public event EventHandler OnReadyChanged;
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
        //调用ClientRpc让所有客户端知道哪个玩家准备好了,只为了ReadyText显示，实际的准备状态是保存在服务器端的playerReadyDictionary中
        SetPlayerReadyClientRpc(serverRpcParams.Receive.SenderClientId);

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
    //让每个客户端都知道其他玩家是否准备好
    [ClientRpc]
    public void SetPlayerReadyClientRpc(ulong clientId)
    {
        playerReadyDictionary[clientId] = true;
        OnReadyChanged?.Invoke(this, EventArgs.Empty);
    }
    //检查某个玩家是否准备好
    public bool IsPlayerReady(ulong clientId)
    {
        return playerReadyDictionary.ContainsKey(clientId) && playerReadyDictionary[clientId];
    }
}
