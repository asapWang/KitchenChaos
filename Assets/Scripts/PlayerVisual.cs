using NUnit.Framework;
using UnityEngine;

public class PlayerVisual : MonoBehaviour
{
    private Animator animator;
    [SerializeField] private Player player;
    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        animator.SetBool("IsMoving", player.IsMoving());
    }
}
