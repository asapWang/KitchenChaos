using UnityEngine;
using TMPro;
using System;

public class CountingDownUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textCountingDown;
    private void Awake()
    {
        Hide();
    }
    private void Start()
    {
        GameManager.instance.OnStateChanged += GameManager_OnStateChanged;
    }
    private void Update()
    {
        textCountingDown.text = Mathf.Ceil(GameManager.instance.GetCountingDownTime()).ToString();
    }
    private void GameManager_OnStateChanged(object sender, EventArgs e)
    {
        if (GameManager.instance.IsCountingDown())
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
        textCountingDown.gameObject.SetActive(false);
    }
    private void Show()
    {
        textCountingDown.gameObject.SetActive(true);
    }
}
