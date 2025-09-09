using UnityEngine;


public class Player : MonoBehaviour
{
    [SerializeField] private float moveSpeed;
    private float rotationSpeed = 10f;
    private bool isMoving;
    [SerializeField] private InputSystem inputSystem;
    private bool canMove;
    private void Update()
    {
        //获取输入并旋转（不论能不能移动）
        Vector2 movePosition = inputSystem.GetMovementInput();
        Vector3 realMovePosition = new Vector3(movePosition.x, 0, movePosition.y);
        transform.forward = Vector3.Slerp(transform.forward, realMovePosition, Time.deltaTime * rotationSpeed);
        //碰撞检测
        float playerHeight = 2f;
        float playerRadius = 0.7f;
        float moveDistance = moveSpeed * Time.deltaTime;
        canMove = !Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHeight, playerRadius, realMovePosition, moveDistance);
        if (!canMove)
        {
            canMove = !Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHeight, playerRadius, new Vector3(realMovePosition.x, 0, 0), moveDistance);
            if (canMove)
            {
                realMovePosition = new Vector3(realMovePosition.x, 0, 0).normalized;
            }
            else
            {
                canMove = !Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHeight, playerRadius, new Vector3(0, 0, realMovePosition.z), moveDistance);
                if (canMove)
                {
                    realMovePosition = new Vector3(0, 0, realMovePosition.z).normalized;
                }
                else
                {
                    realMovePosition = Vector3.zero;
                }
            }
        }
        
        //是否移动
        isMoving = movePosition != Vector2.zero;
        transform.position += realMovePosition * moveDistance;
        
    }
    public bool IsMoving()
    {
        return isMoving;
    }
}
