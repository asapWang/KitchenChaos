using UnityEngine;

public class CuttingCounterVisual : MonoBehaviour
{
    [SerializeField] private CuttingCounter cuttingCounter;
    private Animator animator;
    private const string CUT = "Cut";
    public void Awake()
    {
        animator = GetComponent<Animator>();
    }
    public void Start()
    {
        cuttingCounter.OnCutting += CuttingCounter_OnCutting;
    }
    private void CuttingCounter_OnCutting(object sender, System.EventArgs e)
    {
        animator.SetTrigger(CUT);
    }
}
