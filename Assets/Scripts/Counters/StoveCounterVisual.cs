using UnityEngine;

public class StoveCounterVisual : MonoBehaviour
{
    [SerializeField] private GameObject stove;
    [SerializeField] private GameObject Particles;
    [SerializeField] private StoveCounter stoveCounter;
    private void Start()
    {
        stoveCounter.OnStoveVisualChange += StoveCounter_OnStoveVisualChange;
        
    }
    private void StoveCounter_OnStoveVisualChange(object sender, StoveCounter.OnStoveVisualChangeEventArgs e)
    {
        if(e.state== StoveCounter.State.Frying|| e.state== StoveCounter.State.Fried)
        {
            stove.SetActive(true);
            Particles.SetActive(true);
        }
        else 
        {
            stove.SetActive(false);
            Particles.SetActive(false);
        }
        
    }

}
