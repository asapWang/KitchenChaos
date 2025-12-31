using UnityEngine;

public class ContainerCounterVisual : MonoBehaviour
{
    [SerializeField] private ContainerCounter containerCounter;
    private Animator animator;
    private const string OPEN_CLOSE = "OpenClose";
    public void Awake()
    {
        animator = GetComponent<Animator>();
    }
    public void Start()
    {
        containerCounter.OnOpenContainer += ContainerCounter_OnOpenContainer;
    }
    private void ContainerCounter_OnOpenContainer(object sender, System.EventArgs e)
    {
        animator.SetTrigger(OPEN_CLOSE);
    }
}
