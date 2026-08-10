using System;
using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.SceneManagement;
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }
    //游戏状态改变事件
    public event EventHandler OnStateChanged;
    //本地暂停与恢复游戏事件，作用就是show或hide暂停面板
    public event EventHandler OnLocalPauseGame;
    public event EventHandler OnLocalUnpauseGame;
    //多人暂停与恢复游戏事件，作用就是show或hide等待他人面板，游戏暂停且本地也暂停，暂定面板会盖住等待他人面板
    public event EventHandler OnMultiplayerPauseGame;
    public event EventHandler OnMultiplayerUnpauseGame;
    //玩家准备状态改变事件
    public event EventHandler OnLocalPlayerReadyChanged;
    //游戏状态枚举
    private enum State
    {
        waiting,
        countingDown,
        playing,
        over,
    }
    //同步游戏状态
    private NetworkVariable<State> state= new NetworkVariable<State>(State.waiting);
    //暂停游戏
    //本地是否暂停
    private bool isLocalPaused = false;
    //游戏是否暂停
    private NetworkVariable<bool> isPaused = new NetworkVariable<bool>(false);
    //时间变量
    //同步时间
    private NetworkVariable<float> countingDownTime = new NetworkVariable<float>(3f);
    private NetworkVariable<float> playingTime = new NetworkVariable<float>(10f);
    private float playingTimeMax = 10f;
    private bool isPlayerReady = false;
    //字典用于存储每个玩家的准备状态，只让Server修改;`ulong` 是 C# 的 64 位无符号整数,是ClientId的类型
    private Dictionary<ulong, bool> playerReadyDictionary; 
    //字典存储每个玩家是否pause
    private Dictionary<ulong, bool> playerPausedDictionary;
    //判断是否要检查一遍各玩家暂没暂停
    private bool autoTestAllPlayersPaused = false;
    [SerializeField] private Transform playerPrefabTransform;
    private void Awake()
    {
        Instance = this;
        playerReadyDictionary = new Dictionary<ulong, bool>();
        playerPausedDictionary = new Dictionary<ulong, bool>();
    }
    private void Start()
    {
        InputSystem.Instance.OnPauseAction += InputSystem_OnPauseAction;
        //当玩家按下交互键时，开始游戏
        InputSystem.Instance.OnInteractAction += InputSystem_OnInteractAction;
    }
    public override void OnNetworkSpawn()
    {
        state.OnValueChanged += State_OnValueChanged; 
        isPaused.OnValueChanged += IsPaused_OnValueChanged;
        
        if (IsServer)
        {
            //当玩家断开连接时，重新检查所有玩家是否暂停游戏,防止玩家暂停状态下退出游戏，导致其他玩家无法恢复游戏
            NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
            //当玩家加载完场景时，生成玩家对象
            NetworkManager.Singleton.SceneManager.OnLoadEventCompleted += NetworkManager_OnLoadEventCompleted;
        }
    }
    private void State_OnValueChanged(State previousValue, State newValue)
    {
        OnStateChanged?.Invoke(this, new EventArgs());
    }
    private void IsPaused_OnValueChanged(bool previousValue, bool newValue)
    {
        if (isPaused.Value)
        {
            Time.timeScale = 0;
            //游戏暂停了，显示等待他人面板
            OnMultiplayerPauseGame?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Time.timeScale = 1;
            OnMultiplayerUnpauseGame?.Invoke(this, EventArgs.Empty);
        }
    }
    //不直接调用TestAllPlayersPaused()，而是设置一个标志位，在LateUpdate中调用TestAllPlayersPaused()，因为这个方法执行时玩家还没有被销毁
    private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
    {
        autoTestAllPlayersPaused = true;
    }
    private void NetworkManager_OnLoadEventCompleted(string sceneName, LoadSceneMode loadSceneMode, List<ulong> clientsCompleted, List<ulong> clientsTimedOut)
    {
        foreach (ulong clientId in clientsCompleted)
        {
            Transform playerTransform = Instantiate(playerPrefabTransform);
            playerTransform.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        }
    }
    private void Update()
    {
        if(!IsServer)
        {
            return;
        }
        switch (state.Value)
        {
            case State.waiting:
                break;
            case State.countingDown:
                countingDownTime.Value -= Time.deltaTime;
                if (countingDownTime.Value <= 0)
                {
                    state.Value = State.playing;
                }
                break;
            case State.playing:
                playingTime.Value -= Time.deltaTime;
                if (playingTime.Value <= 0)
                {
                    state.Value = State.over;
                }
                break;
            case State.over:
                break;
        }
    }
    private void LateUpdate()
    {
        if (NetworkManager.Singleton == null ||
            NetworkManager.Singleton.ShutdownInProgress)
        {
            return;
        }
        if (autoTestAllPlayersPaused)
        {
            TestAllPlayersPaused();
            autoTestAllPlayersPaused = false;
        }
    }
    private void InputSystem_OnInteractAction(object sender, EventArgs e)
    {
        if (state.Value == State.waiting)
        {
            isPlayerReady = true;
            OnLocalPlayerReadyChanged?.Invoke(this, new EventArgs());  
            SetPlayerReadyServerRpc(); 
        }
    }
    //Server判断每个玩家是否准备好
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
            state.Value = State.countingDown;
        }
    }
    //判断游戏是否Playing
    public bool IsPlaying()
    {
        return state.Value == State.playing;
    }
    //判断游戏是否CountingDown
    public bool IsCountingDown()
    {
        return state.Value == State.countingDown;
    }
    //判断游戏是否Waiting
    public bool IsWaiting()
    {
        return state.Value == State.waiting;
    }
    //判断游戏是否Over
    public bool IsOver()
    {
        return state.Value == State.over;
    }
    //判断玩家是否准备好
    public bool IsPlayerReady()
    {
        return isPlayerReady;
    }
    //获取countingDownTime
    public float GetCountingDownTime()
    {
        return countingDownTime.Value;
    }
    //获取playingTimeNormalized
    public float GetPlayingTimeNormalized()
    {
        return 1 - (playingTime.Value / playingTimeMax);
    }
    //暂停游戏
    public void TogglePauseGame()
    {
        isLocalPaused = !isLocalPaused;
        if (isLocalPaused)
        {
            PauseGameServerRpc();
            //本地暂停了，显示暂停面板
            OnLocalPauseGame?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            UnpauseGameServerRpc();
            OnLocalUnpauseGame?.Invoke(this, EventArgs.Empty);
        }
    }
    //记录并判断每个玩家是否暂停游戏
    [ServerRpc(RequireOwnership = false)]
    private void PauseGameServerRpc(ServerRpcParams serverRpcParams = default)
    {
        playerPausedDictionary[serverRpcParams.Receive.SenderClientId] = true;
        TestAllPlayersPaused();
    }
    [ServerRpc(RequireOwnership = false)]
    private void UnpauseGameServerRpc(ServerRpcParams serverRpcParams = default)
    {
        playerPausedDictionary[serverRpcParams.Receive.SenderClientId] = false;
        TestAllPlayersPaused();
    }
    //判断所有玩家是否都暂停游戏
    public void TestAllPlayersPaused()
    {
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (playerPausedDictionary.ContainsKey(clientId) && playerPausedDictionary[clientId])
            {
                isPaused.Value = true;
                return;
            }
        }
        isPaused.Value = false;
        return;
    }
    private void InputSystem_OnPauseAction(object sender, EventArgs e)
    {
        //调用暂停游戏方法
        TogglePauseGame();
    }
}
