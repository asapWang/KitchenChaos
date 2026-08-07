using System;
using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }
    //游戏状态改变事件
    public event EventHandler OnStateChanged;
    //暂停游戏事件
    public event EventHandler OnPauseGame;
    //恢复游戏事件
    public event EventHandler OnResumeGame;
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
    private bool isPaused = false;
    //时间变量
    //同步时间
    private NetworkVariable<float> countingDownTime = new NetworkVariable<float>(3f);
    private NetworkVariable<float> playingTime = new NetworkVariable<float>(10f);
    private float playingTimeMax = 10f;
    private bool isPlayerReady = false;
    //字典用于存储每个玩家的准备状态，只让Server修改;`ulong` 是 C# 的 64 位无符号整数,是ClientId的类型
    private Dictionary<ulong, bool> playerReadyDictionary; 
    private void Awake()
    {
        Instance = this;
        playerReadyDictionary = new Dictionary<ulong, bool>();
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
    }
    private void State_OnValueChanged(State previousValue, State newValue)
    {
        OnStateChanged?.Invoke(this, new EventArgs());
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
        isPaused = !isPaused;
        if (isPaused)
        {
            //改变时间缩放为0
            Time.timeScale = 0;
            //监听暂停游戏事件
            OnPauseGame?.Invoke(this, EventArgs.Empty);
        }
        else
        {
            Time.timeScale = 1;
            //监听恢复游戏事件
            OnResumeGame?.Invoke(this, EventArgs.Empty);
        }
    }
    private void InputSystem_OnPauseAction(object sender, EventArgs e)
    {
        //调用暂停游戏方法
        TogglePauseGame();
    }
}
