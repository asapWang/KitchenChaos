using UnityEngine;

public class StoveCounterSound : MonoBehaviour
{
    [SerializeField] private StoveCounter stoveCounter;
    private AudioSource audioSource;
    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }
    private void Start()
    {
        stoveCounter.OnStoveVisualChange += StoveCounter_OnStoveVisualChange;
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
}
