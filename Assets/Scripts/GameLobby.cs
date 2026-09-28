using UnityEngine;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;
using Unity.Services.Core;
using Unity.Services.Authentication;
using System;
using System.Collections.Generic;

public class GameLobby : MonoBehaviour
{
    public static GameLobby Instance { get; private set; }
    //一些事件，LobbyMessageUI可以订阅这些事件来显示不同的UI
    public event EventHandler onCreateLobbyStarted; 
    public event EventHandler onCreateLobbyFailed;
    public event EventHandler onJoinLobbyStarted;
    public event EventHandler onJoinLobbyFailed;
    public event EventHandler onQuickJoinLobbyFailed;
    public event EventHandler<LobbyListChangedEventArgs> onLobbyListChanged;
    //为onListLobbies事件调用传入类
    public class LobbyListChangedEventArgs : EventArgs
    {
        public List<Lobby> lobbyList;
    }
    private Lobby joinedLobby;
    private float heartbeatTimer=15f;
    private float listLobbyTimer;
    private void Awake()
    {
        Instance = this;
        //跨场景不销毁
        DontDestroyOnLoad(gameObject);
        InitializeUnityServices();
    }

    private void Update()
    {
        HandleHeartbeat();
        UpdateLobbyList();
    }

    //发送heartbeat保活
    private async void HandleHeartbeat()
    {
        if (IsHost() && joinedLobby != null)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer <= 0f)
            {
                heartbeatTimer = 15f;
                await LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
            }
        }
    }
    //固定时间刷新lobby列表
    private void UpdateLobbyList()
    {
        //因为这个函数在update里调用，所以要先等已经登录完成，否则用不了listLobbies()里的api
        if(joinedLobby == null && AuthenticationService.Instance.IsSignedIn)
        {
            listLobbyTimer -= Time.deltaTime;
            if (listLobbyTimer <= 0f)
            {
                listLobbyTimer = 5f;
                ListLobbies();
            }
        }
        
    }

    //创建和加入lobby前，先启动Unity Services并登录
    private async void InitializeUnityServices()
    {
        //避免重复初始化
        if (UnityServices.State == ServicesInitializationState.Initialized)
        {
            return;
        }
        //下面这个options是给初始化配置对象
        //匿名登录不等于每次都创建新PlayerId，两个本地游戏实例若使用默认 Profile，且共享同一份本地认证缓存，就会读到同一个匿名 Session Token，因此登录成相同的 PlayerId
        //SetProfile() 不会直接设置云端 PlayerId，也不会自己登录；它只是先切换本地凭据的存储范围。之后的匿名登录才会从该范围恢复或创建身份。
        /*
        Profile
        → 决定本地去哪个“凭据存储格”读取 Session Token
        → SignInAnonymouslyAsync()
        ├─ 找到该 Profile 旧 Token：恢复原来的匿名玩家和 PlayerId
        └─ 找不到 Token：创建新的匿名玩家和新的 PlayerId
        */
        InitializationOptions options = new InitializationOptions();
        options.SetProfile(UnityEngine.Random.Range(0, 100000).ToString());
        await UnityServices.InitializeAsync(options);
        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }
    //创建lobby
    public async void CreateLobby(string lobbyName, bool isPrivate)
    {
        onCreateLobbyStarted?.Invoke(this, EventArgs.Empty);
        try
        {
            //API 的设计习惯：必填、最核心的数据用方法参数；可选或会不断扩展的配置放进 Options 对象。
            joinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, GameMultiplayer.MAX_PLAYERS_AMOUNT, new CreateLobbyOptions
            {
                IsPrivate = isPrivate
            });
            GameMultiplayer.Instance.StartHost();
            Loader.LoadNetwork(Loader.Scene.CharacterSelectScene);
        }
        catch (System.Exception e)
        {
            onCreateLobbyFailed?.Invoke(this, EventArgs.Empty);
            Debug.LogError(e.Message);   
        }
        
    }
    //快速加入lobby
    public async void QuickJoinLobby()
    {
        onJoinLobbyStarted?.Invoke(this, EventArgs.Empty);
        try
        {
            joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync();
            GameMultiplayer.Instance.StartClient();
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.Message);
            onQuickJoinLobbyFailed?.Invoke(this, EventArgs.Empty);
        }
    }
    //通过lobbyCode加入lobby
    public async void JoinLobbyByCode(string lobbyCode)
    {
        onJoinLobbyStarted?.Invoke(this, EventArgs.Empty);
        try
        {
            joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
            GameMultiplayer.Instance.StartClient();
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.Message);
            onJoinLobbyFailed?.Invoke(this, EventArgs.Empty);
        }
    }
    //通过lobbyId加入lobby
    public async void JoinLobbyById(string lobbyId)
    {
        onJoinLobbyStarted?.Invoke(this, EventArgs.Empty);
        try
        {
            joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
            GameMultiplayer.Instance.StartClient();
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.Message);
            onJoinLobbyFailed?.Invoke(this, EventArgs.Empty);
        }
    }
    //列出所有lobby
    public async void ListLobbies()
    {
        try
        {
            //添加过滤条件，剩余人数大于0
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Filters = new System.Collections.Generic.List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                }
            };
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(options);
            onLobbyListChanged?.Invoke(this, new LobbyListChangedEventArgs { lobbyList = queryResponse.Results });
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.Message);
        }
    }


    //获取当前玩家加入的lobby
    public Lobby GetLobby()
    {
        return joinedLobby;
    }
    //删除lobby
    public async void DeleteLobby()
    {
        if(joinedLobby == null)
        {
            return;
        }
        try
        {
            await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);
            joinedLobby = null;
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.Message);
        }
    }
    //离开lobby
    public async void LeaveLobby()
    {
        if(joinedLobby == null)
        {
            return;
        }
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);
            joinedLobby = null;
        }
        catch (System.Exception e)
        {
            Debug.LogError(e.Message);
        }
    }
    //踢出玩家
    public async void KickPlayer(string playerId)
    {
        if(IsHost() && joinedLobby != null)
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerId);
            }
            catch (System.Exception e)
            {
                Debug.LogError(e.Message);
            }
        }
    }









    //判断是否为主机
    public bool IsHost()
    {
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }
}
