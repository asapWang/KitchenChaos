using UnityEngine;

public class StoveBarFlashUI : MonoBehaviour
{
    private Animator animator;
    private const string IS_FLASH = "isFlash";
    [SerializeField] private StoveCounter stoveCounter;
    private void Awake()
    {
        animator = GetComponent<Animator>();
    }
    private void Start()
    {
        stoveCounter.OnProgressBarUIChanged += StoveCounter_OnProgressBarUIChanged;
    }
    private void StoveCounter_OnProgressBarUIChanged(object sender, IHasProgress.OnProgressBarUIChangedEventArgs e)
    {
        bool show = stoveCounter.IsFried() && e.progressNormalized>=0.5f && e.progressNormalized < 1f;
        if (show)
        {
            animator.SetBool(IS_FLASH, true);
        }
        else
        {
            animator.SetBool(IS_FLASH, false);
        }
    }

}
