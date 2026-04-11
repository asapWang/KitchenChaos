using UnityEngine;
using TMPro;
using System;

public class CountingDownUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textCountingDown;
    private int previousCountingDownNumber = -1;
    private Animator animator;
    //避免字符串的直接使用，定义一个常量来存储动画触发器的名称
    private const string NUMBER_POPUP = "numberPopUp";
    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void Start()
    {
        GameManager.Instance.OnStateChanged += GameManager_OnStateChanged;
        Hide();
    }
    private void Update()
    {
        int countingDownNumber = Mathf.CeilToInt(GameManager.Instance.GetCountingDownTime());
        textCountingDown.text = countingDownNumber.ToString();
        //判断倒计时数字是否和上一次显示的数字相同
        if (countingDownNumber != previousCountingDownNumber)
        {
            previousCountingDownNumber = countingDownNumber;
            //当数字发生变化时，播放动画和音效
            animator.SetTrigger(NUMBER_POPUP);
            SoundManager.Instance.PlayNumberPopUpSound();
        }
    }
    private void GameManager_OnStateChanged(object sender, EventArgs e)
    {
        if (GameManager.Instance.IsCountingDown())
        {
            Show();
        }
        else
        {
            Hide();
        }
    }
    private void Hide()
    {
        gameObject.SetActive(false);
    }
    private void Show()
    {
        gameObject.SetActive(true);
    }
}
