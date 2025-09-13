using NUnit.Framework;
using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    private const string IS_MOVING = "IsMoving";
    private Animator animator;
    [SerializeField] private Player player;
    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        animator.SetBool(IS_MOVING, player.IsMoving());
    }
}
