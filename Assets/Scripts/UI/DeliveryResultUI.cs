using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeliveryResultUI : MonoBehaviour
{
    [SerializeField] private Image imgBK;
    [SerializeField] private Image imgIcon;
    [SerializeField] private Sprite successSprite;
    [SerializeField] private Sprite failSprite;
    [SerializeField] private Color successColor;
    [SerializeField] private Color failColor;
    [SerializeField] private TextMeshProUGUI textMessage;
    private Animator animator;
    private const string POP_UP = "popUp";
    private void Awake()
    {
        animator = GetComponent<Animator>();
    }
    private void Start()
    {
        DeliveryManager.Instance.OnDeliverSuccess += DeliveryManager_OnDeliverSuccess;
        DeliveryManager.Instance.OnDeliverFail += DeliveryManager_OnDeliverFail;
        this.gameObject.SetActive(false);
    }
    private void DeliveryManager_OnDeliverFail(object sender, System.EventArgs e)
    {
        this.gameObject.SetActive(true);
        animator.SetTrigger(POP_UP);
        imgBK.color = failColor;
        imgIcon.sprite = failSprite;
        textMessage.text = "DELIVERY\nFAILED";

    }
    private void DeliveryManager_OnDeliverSuccess(object sender, System.EventArgs e)
    {
        this.gameObject.SetActive(true);
        animator.SetTrigger(POP_UP);
        imgBK.color = successColor;
        imgIcon.sprite = successSprite;
        textMessage.text = "DELIVERY\nSUCCESS";
    }
    
}
