using UnityEngine;


public class Player : MonoBehaviour
{
    [SerializeField] private float moveSpeed;
    [SerializeField] private float rotationSpeed;
    private void Update()
    {
        Vector2 movePosition = new Vector2(0, 0);
        if (Input.GetKey(KeyCode.W))
        {
            movePosition.y += 1;
        }
        if (Input.GetKey(KeyCode.A))
        {
            movePosition.x -= 1;
        }
        if (Input.GetKey(KeyCode.S))
        {
            movePosition.y -= 1;
        }
        if (Input.GetKey(KeyCode.D))
        {
            movePosition.x += 1;
        }
        movePosition = movePosition.normalized;
        Vector3 realMovePosition = new Vector3(movePosition.x, 0, movePosition.y);
        transform.position += realMovePosition * moveSpeed * Time.deltaTime;
        transform.forward = Vector3.Slerp(transform.forward, realMovePosition, Time.deltaTime * rotationSpeed);
    }
}
