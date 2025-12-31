using UnityEngine;
using UnityEngine.UI;

public class ProgressBarUI : MonoBehaviour
{
    [SerializeField]private Image image;
    [SerializeField]private GameObject hasProgressGameObject;
    private void Start()
    {
        hasProgressGameObject.GetComponent<IHasProgress>().OnProgressBarUIChanged += HasProgress_OnProgressBarUIChanged;
        
        Hide();
    }
    private void HasProgress_OnProgressBarUIChanged(object sender, IHasProgress.OnProgressBarUIChangedEventArgs e)
    {
        //image填充方法
        image.fillAmount = e.progressNormalized;
        if (e.progressNormalized >= 1f || e.progressNormalized == 0f)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }
    private void Show()
    {
        gameObject.SetActive(true);
    }
    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
