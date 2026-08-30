using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;

public class GameMultiplayer : NetworkBehaviour
{
    [SerializeField] private KitchenObjectListSO kitchenObjectListSO;
    //角色选择颜色
    [SerializeField] private List<Color> playerColorList;
    private const int MAX_PLAYERS_AMOUNT = 4;
    //记录所有客户端的PlayerData数据
    private NetworkList<PlayerData> playerDataNetworkList;
    public static GameMultiplayer Instance { get; private set; }
    //大厅里尝试加入游戏和加入游戏失败的事件
    public EventHandler OnTryingToJoinGame;
    public EventHandler OnFailedToJoinGame;
    //当playerDataNetworkList发生变化时触发的事件
    public EventHandler OnPlayerDataNetworkListChanged;
    private void Awake()
    {
        Instance = this;
        playerDataNetworkList = new NetworkList<PlayerData>();
        playerDataNetworkList.OnListChanged += PlayerDataNetworkList_OnListChanged;
        DontDestroyOnLoad(gameObject);
    }
    private void PlayerDataNetworkList_OnListChanged(NetworkListEvent<PlayerData> changeEvent)
    {
        OnPlayerDataNetworkListChanged?.Invoke(this, EventArgs.Empty);
    }
    //
    public void StartHost()
    {
        NetworkManager.Singleton.ConnectionApprovalCallback += NetworkManager_ConnectionApprovalCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Server_OnClientDisconnectCallback;
        NetworkManager.Singleton.StartHost();
    }
    private void NetworkManager_ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        //如果不在CharacterSelectScene场景，拒绝连接
        if (SceneManager.GetActiveScene().name != Loader.Scene.CharacterSelectScene.ToString())
        {
            response.Approved = false;
            response.Reason = "Game has already started.";
            return;
        }
        //如果超过最大连接数，拒绝连接
        if (NetworkManager.Singleton.ConnectedClients.Count >= MAX_PLAYERS_AMOUNT)
        {
            response.Approved = false;
            response.Reason = "Server is full.";
            return;
        }
        response.Approved = true;
    }
    private void NetworkManager_OnClientConnectedCallback(ulong clientId)
    {
        //新玩家加入时，记录他的playerData，包括clientId和一个可用的颜色ID
        playerDataNetworkList.Add(new PlayerData { clientId = clientId, colorID = GetAvailableColorID() });
    }
    private void NetworkManager_Server_OnClientDisconnectCallback(ulong clientId)
    {
        //玩家退出时，在服务端删除他的playerData，这样CharacterSelectPlayer就会被销毁，其他客户端也会同步销毁
        int playerDataIndex = GetPlayerDataIndexFromClientId(clientId);
        if (playerDataIndex != -1)
        {
            playerDataNetworkList.RemoveAt(playerDataIndex);
        }
    }

    public void StartClient()
    {
        //尝试创建客户端，总是会触发OnTryingToJoinGame事件，客户端连接失败时会触发OnFailedToJoinGame事件
        OnTryingToJoinGame?.Invoke(this, EventArgs.Empty);
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Client_OnClientDisconnectCallback;
        NetworkManager.Singleton.StartClient();
    }
    private void NetworkManager_Client_OnClientDisconnectCallback(ulong clientId)
    {
        OnFailedToJoinGame?.Invoke(this, EventArgs.Empty);
    }
    
    //KitchenObject脚本调用这个方法继而调用ServerRpc来生成KitchenObject实例并同步
    public void SpawnKitchenObject(KitchenObjectSO kitchenObjectSO, IGetKitchenObject iKitchenObjectParent)
    {
        //通过转换，把KitchenObjectSO转换成索引和把父物体转换为NetworkObject，传递给RPC方法
        SpawnKitchenObjectServerRpc(GetKitchenObjectSOIndex(kitchenObjectSO), iKitchenObjectParent.GetNetworkObject());
    }
    [ServerRpc(RequireOwnership = false)]
    //RPC方法的参数不能是引用类型，所以传递KitchenObjectSO的索引和结构体NetworkObjectReference，此结构体可以接受NetworkObject作为参数，并在RPC方法中通过TryGet方法获取NetworkObject
    public void SpawnKitchenObjectServerRpc(int kitchenObjectSOIndex, NetworkObjectReference ikitchenObjectParentNetworkObjectReference)
    {
        //Instantiate会生成实例，并把第一个transform变成第二个transform的子物体，返回值是第一个transform，也可以不指定父对象
        Transform kitchenObjectTransform = Instantiate(GetKitchenObjectSOFromIndex(kitchenObjectSOIndex).kitchenObjectPrefab.transform);
        //Spawn方法会在所有客户端生成实例
        KitchenObject kitchenObject = kitchenObjectTransform.GetComponent<KitchenObject>();
        kitchenObject.NetworkObject.Spawn();
        //通过NetworkObjectReference获取NetworkObject，再通过GetComponent获取IGetKitchenObject类型的父对象
        ikitchenObjectParentNetworkObjectReference.TryGet(out NetworkObject ikitchenObjectParentNetworkObject);
        IGetKitchenObject ikitchenObjectParent = ikitchenObjectParentNetworkObject.GetComponent<IGetKitchenObject>();
        kitchenObject.SetOwner(ikitchenObjectParent);
    }

    //KitchenObject脚本调用这个方法继而调用ServerRpc来销毁KitchenObject实例并同步
    public void DestroyKitchenObject(KitchenObject kitchenObject)
    {
        DestroyKitchenObjectServerRpc(kitchenObject.NetworkObject);
    }
    [ServerRpc(RequireOwnership = false)]
    public void DestroyKitchenObjectServerRpc(NetworkObjectReference kitchenObjectNetworkObjectReference)
    {
        kitchenObjectNetworkObjectReference.TryGet(out NetworkObject kitchenObjectNetworkObject);
        KitchenObject kitchenObject = kitchenObjectNetworkObject.GetComponent<KitchenObject>();
        //销毁KitchenObject实例前，先清楚父对象对kitchenObject的引用
        ClearKitchenObjectClientRpc(kitchenObject.NetworkObject);
        kitchenObject.DestroySelf();
    }
    //清楚父对象对kitchenObject的引用
    [ClientRpc]
    public void ClearKitchenObjectClientRpc(NetworkObjectReference kitchenObjectNetworkObjectReference)
    {
        kitchenObjectNetworkObjectReference.TryGet(out NetworkObject kitchenObjectNetworkObject);
        KitchenObject kitchenObject = kitchenObjectNetworkObject.GetComponent<KitchenObject>();
        kitchenObject.ClearKitchenObjectOnParent();
    }

    //设置玩家颜色，就是改变PlayerData的colorID，其余交给CharacterSelectPlayer来做
    public void SetPlayerColor(int colorID)
    {
        SetPlayerColorServerRpc(colorID);
    }
    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerColorServerRpc(int colorID, ServerRpcParams serverRpcParams = default)
    {
        //先判断这个颜色有没有被其他玩家使用
        if (IsColorUsed(colorID))
        {
            return;
        }
        //因为playerData是结构体，所以是值类型，修改playerData的colorID不会影响playerDataNetworkList里的数据，所以需要新建一个playerData来修改colorID，然后再把修改后的playerData写回playerDataNetworkList
        //获取调用这个ServerRpc的客户端的PlayerData
        PlayerData playerData = GetPlayerDataFromClientId(serverRpcParams.Receive.SenderClientId);
        //修改PlayerData的colorID
        playerData.colorID = colorID;
        //把修改后的PlayerData写回playerDataNetworkList
        playerDataNetworkList[GetPlayerDataIndexFromClientId(serverRpcParams.Receive.SenderClientId)] = playerData; 
    }
    








    
    //得到KitchenObjectSO的索引
    public int GetKitchenObjectSOIndex(KitchenObjectSO kitchenObjectSO)
    {
        return kitchenObjectListSO.kitchenObjectSOList.IndexOf(kitchenObjectSO);
    }
    //根据索引得到KitchenObjectSO
    public KitchenObjectSO GetKitchenObjectSOFromIndex(int index)
    {
        return kitchenObjectListSO.kitchenObjectSOList[index];
    }
    //判断某索引CharacterSelectPlayer是否存在
    public bool IsPlayerIndexConnected(int index)
    {
        return index < playerDataNetworkList.Count;
    }
    //根据索引获取CharacterSelectPlayer的PlayerData
    public PlayerData GetPlayerDataFromIndex(int index)
    {
        return playerDataNetworkList[index];
    }
    //根据localClientId获取PlayerData
    public PlayerData GetPlayerDataFromClientId(ulong clientId)
    {
        foreach (PlayerData playerData in playerDataNetworkList)
        {
            if (playerData.clientId == clientId)
            {
                return playerData;
            }
        }
        return default;
    }
    //获取某个PlayerData
    public PlayerData GetPlayerData()
    {
        return GetPlayerDataFromClientId(NetworkManager.Singleton.LocalClientId);
    }
    //根据ID获取颜色
    public Color GetPlayerColor(int playerIndex)
    {
        return playerColorList[playerIndex];
    }
    //判断某个颜色是否被使用
    public bool IsColorUsed(int colorID)
    {
        foreach (PlayerData playerData in playerDataNetworkList)
        {
            if (playerData.colorID == colorID)
            {
                return true;
            }
        }
        return false;
    }
    //根据clientId获取playerDataNetworkList的索引
    public int GetPlayerDataIndexFromClientId(ulong clientId)
    {
        for (int i = 0; i < playerDataNetworkList.Count; i++)
        {
            if (playerDataNetworkList[i].clientId == clientId)
            {
                return i;
            }
        }
        return -1;
    }
    //新加入的玩家获得一个可以使用的颜色ID
    public int GetAvailableColorID()
    {
        for (int i = 0; i < playerColorList.Count; i++)
        {
            if (!IsColorUsed(i))
            {
                return i;
            }
        }
        return -1;
    }
    //踢掉玩家
    public void KickPlayer(ulong clientId)
    {
        NetworkManager.Singleton.DisconnectClient(clientId);
        //有时候主动断联DisconnectClient不会触发OnClientDisconnectCallback，所以手动触发
        NetworkManager_Server_OnClientDisconnectCallback(clientId);
    }
}
