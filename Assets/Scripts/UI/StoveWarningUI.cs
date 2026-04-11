using UnityEngine;

public class StoveWarningUI : MonoBehaviour
{
    [SerializeField] private StoveCounter stoveCounter;
    private void Start()
    {
        stoveCounter.OnProgressBarUIChanged += StoveCounter_OnProgressBarUIChanged;
        Hide();
    }
    private void StoveCounter_OnProgressBarUIChanged(object sender, IHasProgress.OnProgressBarUIChangedEventArgs e)
    {
        bool show = stoveCounter.IsFried() && e.progressNormalized>=0.5f && e.progressNormalized < 1f;
        if (show)
        {
            Show();
        }
        else
        {
            Hide();
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
