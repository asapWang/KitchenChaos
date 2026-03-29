using UnityEngine;
using UnityEngine.UI;

public class ClockUI : MonoBehaviour
{
    [SerializeField] private Image imageClock;
    private void Update()
    {
        imageClock.fillAmount = GameManager.Instance.GetPlayingTimeNormalized();
    }
}
