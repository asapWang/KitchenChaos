using UnityEngine;


public class Player : MonoBehaviour
{
    [SerializeField] private float moveSpeed;
    private float rotationSpeed = 10f;
    private bool isMoving;
    [SerializeField] private InputSystem inputSystem;
    private void Update()
    {
        Vector2 movePosition = inputSystem.GetMovementInput();
        isMoving = movePosition != Vector2.zero;
        movePosition = movePosition.normalized;
        Vector3 realMovePosition = new Vector3(movePosition.x, 0, movePosition.y);
        transform.position += realMovePosition * moveSpeed * Time.deltaTime;
        transform.forward = Vector3.Slerp(transform.forward, realMovePosition, Time.deltaTime * rotationSpeed);
    }
    public bool IsMoving()
    {
        return isMoving;
    }
}
