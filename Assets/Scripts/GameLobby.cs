using UnityEngine;
using Unity.Services.Lobbies.Models;
using Unity.Services.Lobbies;
using Unity.Services.Core;
using Unity.Services.Authentication;
using UnityEngine.SceneManagement;
using System;
using System.Collections.Generic;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;


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
    private const string KEY_RELAY_JOIN_CODE = "RelayJoinCode";
    private Lobby joinedLobby;
    private float heartbeatTimer=15f;
    private float listLobbyTimer;
    private Task servicesReadyTask;
    private bool isCreatingLobby;
    private async void Awake()
    {
        Instance = this;
        //跨场景不销毁
        DontDestroyOnLoad(gameObject);
        try
        {
            await EnsureServicesReadyAsync();
            Debug.Log("UGS 认证就绪");
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }

    private void Update()
    {
        HandleHeartbeat();
        HandlePeriodUpdateLobbyList();
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
    private void HandlePeriodUpdateLobbyList()
    {
        //因为这个函数在update里调用，所以要先等初始化和登录完成，否则用不了listLobbies()里的api
        //进入游戏场景后，joinedLobby为null，但不再需要刷新lobby列表
        if(joinedLobby == null &&
        UnityServices.State == ServicesInitializationState.Initialized &&
        AuthenticationService.Instance.IsAuthorized &&
        SceneManager.GetActiveScene().name == Loader.Scene.LobbyScene.ToString())
        {
            listLobbyTimer -= Time.deltaTime;
            if (listLobbyTimer <= 0f)
            {
                listLobbyTimer = 5f;
                ListLobbies();
            }
        }
    }

    //创建Relay
    private async Task<Allocation> AllocateRelay()
    {
        // 失败交给 CreateLobby 统一处理，不能返回 null 后继续创建流程。
        return await RelayService.Instance.CreateAllocationAsync(GameMultiplayer.MAX_PLAYERS_AMOUNT - 1);
    }
    //获取Relay join code
    private async Task<string> GetRelayJoinCode(Allocation allocation)
    {
        return await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
    }

    //创建和加入lobby前，先初始化Unity Services并登录
    private async Task InitializeAndSignInAsync()
    {
        //判断是否初始化成功
        if (UnityServices.State != ServicesInitializationState.Initialized)
        {
            InitializationOptions options = new InitializationOptions();
            options.SetProfile(UnityEngine.Random.Range(0, 100000).ToString());
            await UnityServices.InitializeAsync(options);
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
        
        //判断是否登录成功，登录与初始化不是同一回事，初始化只是让Unity Services的SDK可以使用，而登录是获取PlayerId和Session Token
        if (!AuthenticationService.Instance.IsAuthorized)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
    }
    //创建lobby
    public async void CreateLobby(string lobbyName, bool isPrivate)
    {
        if (isCreatingLobby || joinedLobby != null)
        {
            return;
        }

        // 在事件通知和第一个 await 之前占用入口，避免重复点击或事件重入。
        isCreatingLobby = true;
        try
        {
            onCreateLobbyStarted?.Invoke(this, EventArgs.Empty);
            await CreateLobbyAsync(lobbyName, isPrivate);
        }
        catch (System.Exception e)
        {
            onCreateLobbyFailed?.Invoke(this, EventArgs.Empty);
            Debug.LogError(e.Message);
        }
        finally
        {
            // CreateLobbyAsync 包含失败回滚，等待它结束后才允许再次创建。
            isCreatingLobby = false;
        }
    }

    // 核心流程返回 Task，调用方可以等待它完成并接收异常。
    private async Task CreateLobbyAsync(string lobbyName, bool isPrivate)
    {
        // 记录本次操作创建的资源，失败时不能依赖可能已经变化的 joinedLobby。
        Lobby createdLobby = null;
        GameMultiplayer hostToRollback = null;
        try
        {
            await EnsureServicesReadyAsync();
            //API 的设计习惯：必填、最核心的数据用方法参数；可选或会不断扩展的配置放进 Options 对象。
            createdLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, GameMultiplayer.MAX_PLAYERS_AMOUNT, new CreateLobbyOptions
            {
                IsPrivate = isPrivate
            });
            joinedLobby = createdLobby;
            //必须在NGO创建主机前先创建Relay
            Allocation allocation = await AllocateRelay();
            string relayJoinCode = await GetRelayJoinCode(allocation);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(allocation, "dtls"));
            //把Relay join code存入lobby的Data里，方便客户端加入lobby后获取join code来连接Relay
            joinedLobby = await LobbyService.Instance.UpdateLobbyAsync(createdLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_RELAY_JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
                }
            });
            NetworkManager manager = NetworkManager.Singleton;
            GameMultiplayer multiplayer = GameMultiplayer.Instance;
            // StartHost 内的 RPC 可能在 NGO 启动后抛异常，因此必须提前记录。
            // 已有会话时不取得清理责任，交给 StartHost 的检查拒绝本次启动。
            if (manager != null && !manager.IsListening && !manager.IsClient &&
                !manager.IsServer && !manager.ShutdownInProgress)
            {
                hostToRollback = multiplayer;
            }
            multiplayer.StartHost();
            SceneEventProgressStatus loadStatus = Loader.LoadNetwork(Loader.Scene.CharacterSelectScene);
            if (loadStatus != SceneEventProgressStatus.Started)
            {
                throw new InvalidOperationException($"Failed to start loading CharacterSelectScene: {loadStatus}");
            }
        }
        catch
        {
            await RollbackCreationAsync(createdLobby, hostToRollback);
            throw;
        }
    }

    private async Task RollbackCreationAsync(Lobby createdLobby, GameMultiplayer hostToRollback)
    {
        if (hostToRollback != null)
        {
            try
            {
                await hostToRollback.ShutdownNetworkAsync();
            }
            catch (Exception e)
            {
                // NGO 停止失败也不能阻断 Lobby 清理或覆盖原始错误。
                Debug.LogError($"Failed to stop host during lobby rollback: {e.Message}");
            }
        }
        await RollbackCreatedLobbyAsync(createdLobby);
    }

    private async Task RollbackCreatedLobbyAsync(Lobby createdLobby)
    {
        if (createdLobby == null)
        {
            return;
        }

        // 先解除本地引用，停止对失败房间发起新的心跳；不清空其他操作的房间。
        if (joinedLobby != null && joinedLobby.Id == createdLobby.Id)
        {
            joinedLobby = null;
        }

        try
        {
            await LobbyService.Instance.DeleteLobbyAsync(createdLobby.Id);
        }
        catch (LobbyServiceException e) when (e.Reason == LobbyExceptionReason.LobbyNotFound)
        {
            // 房间已经不存在，清理目标已达到。
        }
        catch (Exception e)
        {
            // 清理失败不能覆盖创建流程最初的异常。
            Debug.LogError($"Failed to clean up lobby {createdLobby.Id}: {e.Message}");
        }
    }
    //快速加入lobby
    public async void QuickJoinLobby()
    {
        onJoinLobbyStarted?.Invoke(this, EventArgs.Empty);
        try
        {
            await EnsureServicesReadyAsync();
            joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync();
            //获取lobby里的Relay join code
            string relayJoinCode = joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;
            //通过join code加入Relay
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, "dtls"));
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
            await EnsureServicesReadyAsync();
            joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
            //获取lobby里的Relay join code
            string relayJoinCode = joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;
            //通过join code加入Relay
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, "dtls"));
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
            await EnsureServicesReadyAsync();
            joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
            //获取lobby里的Relay join code
            string relayJoinCode = joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value;
            //通过join code加入Relay
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(relayJoinCode);
            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, "dtls"));
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
                Filters = new List<QueryFilter>
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
    //共享认证任务
    /*逻辑是：
    - 没有任务：启动认证。
    - 任务未结束：返回正在执行的任务。
    - 任务已经结束：重新检查初始化和授权；需要时重试。
    成功后再次调用，也不会必然重复登录，因为内部已有 IsAuthorized 检查
    */
    // 可替换执行操作以便测试；正常调用仍使用真实 UGS 认证。
    private Task EnsureServicesReadyAsync(Func<Task> initializeAndSignIn = null)
    {
        if (servicesReadyTask == null || servicesReadyTask.IsCompleted)
        {
            servicesReadyTask = (initializeAndSignIn ?? InitializeAndSignInAsync)();
        }

        return servicesReadyTask;
    }
}
