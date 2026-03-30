using System;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    //游戏状态改变事件
    public event EventHandler OnStateChanged;
    //暂停游戏事件
    public event EventHandler OnPauseGame;
    //恢复游戏事件
    public event EventHandler OnResumeGame;
    //游戏状态枚举
    private enum State
    {
        waiting,
        countingDown,
        playing,
        over,
    }
    private State state;
    //暂停游戏
    private bool isPaused = false;
    //状态时间变量
    private float waitingTime = 1f;
    private float countingDownTime = 3f;
    private float playingTimeMax = 10f;
    private float playingTime = 10f;
    private void Awake()
    {
        Instance = this;
        state = State.waiting;
    }
    private void Start()
    {
        InputSystem.Instance.OnPauseAction += InputSystem_OnPauseAction;
    }
    private void Update()
    {
        switch (state)
        {
            case State.waiting:
                waitingTime -= Time.deltaTime;
                if (waitingTime <= 0)
                {
                    state = State.countingDown;
                    OnStateChanged?.Invoke(this, new EventArgs());
                }
                break;
            case State.countingDown:
                countingDownTime -= Time.deltaTime;
                if (countingDownTime <= 0)
                {
                    state = State.playing;
                    OnStateChanged?.Invoke(this, new EventArgs());
                }
                break;
            case State.playing:
                playingTime -= Time.deltaTime;
                if (playingTime <= 0)
                {
                    state = State.over;
                    OnStateChanged?.Invoke(this, new EventArgs());
                }
                break;
            case State.over:
                break;
        }
    }
    //判断游戏是否Playing
    public bool IsPlaying()
    {
        return state == State.playing;
    }
    //判断游戏是否CountingDown
    public bool IsCountingDown()
    {
        return state == State.countingDown;
    }
    //判断游戏是否Over
    public bool IsOver()
    {
        return state == State.over;
    }
    //获取countingDownTime
    public float GetCountingDownTime()
    {
        return countingDownTime;
    }
    //获取playingTimeNormalized
    public float GetPlayingTimeNormalized()
    {
        return 1 - (playingTime / playingTimeMax);
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
