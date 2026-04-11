using UnityEngine;

public class StoveCounterSound : MonoBehaviour
{
    [SerializeField] private StoveCounter stoveCounter;
    private AudioSource audioSource;
    private float warningTime;
    private float warningTimeMax = 0.1f;
    private bool isPlayWarningSound;
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }
    private void Start()
    {
        stoveCounter.OnStoveVisualChange += StoveCounter_OnStoveVisualChange;
        //警告音效
         stoveCounter.OnProgressBarUIChanged += StoveCounter_OnProgressBarUIChanged;
    }
    private void StoveCounter_OnStoveVisualChange(object sender, StoveCounter.OnStoveVisualChangeEventArgs e)
    {
        if(e.state== StoveCounter.State.Frying|| e.state == StoveCounter.State.Fried)
        {
            audioSource.Play();
        }
        else
        {
            audioSource.Pause();
        }
    }
    private void StoveCounter_OnProgressBarUIChanged(object sender, IHasProgress.OnProgressBarUIChangedEventArgs e)
    {
        //是否播放警告音效的条件：处于已烹饪状态，并且进度在0.5到1之间
        //不直接在这里播放警告音效，而是设置一个标志位，在Update方法中根据这个标志位来播放，这样可以添加播放间隔，逻辑更清晰
        isPlayWarningSound = stoveCounter.IsFried() && e.progressNormalized >= 0.5f && e.progressNormalized < 1f;
    }
    private void Update()
    {
        warningTime += Time.deltaTime;
        if (warningTime > warningTimeMax)
        {
            warningTime = 0f;
            if (isPlayWarningSound)
            {
                SoundManager.Instance.PlayWarningSound(stoveCounter.transform.position);
            }
        }
    }
}
