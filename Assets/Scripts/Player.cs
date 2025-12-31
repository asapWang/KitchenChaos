using UnityEngine;
using System;

public class Player : MonoBehaviour, IGetKitchenObject
{
    [SerializeField] private float moveSpeed;
    [SerializeField] private InputSystem inputSystem;
    [SerializeField] private LayerMask counterLayerMask;
    [SerializeField] private Transform playerHandPosition;
    //isMoving只用于动画控制
    private bool isMoving;
    private Vector3 lastMoveDir;
    private BaseCounter selectedCounter;
    //突出SelectedCounterVisual事件
    public event EventHandler<SelectedCounterEventArgs> OnSelectedCounter;
    public class SelectedCounterEventArgs : EventArgs
    {
        public BaseCounter selectedCounter;
    }
    public static Player Instance { get; private set; }
    private KitchenObject kitchenObject;
    
    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There is more than one Player instance!");
        }
        Instance = this;
    }
    private void Start()
    {
        inputSystem.OnInteractAction += InputSystem_OnInteractAction;
        inputSystem.OnInteractAlternativeAction += InputSystem_OnInteractAlternativeAction;
    }

    private void InputSystem_OnInteractAction(object sender, System.EventArgs e)
    {
        if (selectedCounter != null)
        {
            selectedCounter.Interact(this);
        }
    }
    private void InputSystem_OnInteractAlternativeAction(object sender, System.EventArgs e)
    {
        if (selectedCounter != null)
        {
            selectedCounter.InteractAlternative();
        }
    }
    
   
    private void Update()
    {
        HandleMovement();
        HandleInteractions();
    }


    public bool IsMoving()
    {
        return isMoving;
    }

    private void HandleMovement()
    {
        //获取输入并旋转（不论能不能移动）
        Vector2 movePosition = inputSystem.GetMovementInput();
        Vector3 realMovePosition = new Vector3(movePosition.x, 0, movePosition.y);
        float rotationSpeed = 10f;
        transform.forward = Vector3.Slerp(transform.forward, realMovePosition, Time.deltaTime * rotationSpeed);
        //碰撞检测
        float playerHeight = 2f;
        float playerRadius = 0.7f;
        float moveDistance = moveSpeed * Time.deltaTime;
        bool canMove= !Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHeight, playerRadius, realMovePosition, moveDistance);
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

    private void HandleInteractions()
    {
        Vector2 movePosition = inputSystem.GetMovementInput();
        Vector3 realMovePosition = new Vector3(movePosition.x, 0, movePosition.y);
        if (realMovePosition != Vector3.zero)
        {
            lastMoveDir = realMovePosition;
        }
        float rayLength = 2f;

        if (Physics.Raycast(transform.position, lastMoveDir, out RaycastHit raycastHit, rayLength, counterLayerMask))
        {
            if (raycastHit.transform.TryGetComponent(out BaseCounter baseCounter))
            {
                SetSelectedCounter(baseCounter);
            }
            else
            {
                SetSelectedCounter(null);
            }
        }
        else
        {
            SetSelectedCounter(null);
        }
    }

    private void SetSelectedCounter(BaseCounter baseCounter)
    {
        selectedCounter = baseCounter;
        OnSelectedCounter?.Invoke(this, new SelectedCounterEventArgs { selectedCounter = selectedCounter });
    }




    //以下为接口
    public Transform GetKitchenObjectPosition()
    {
        return playerHandPosition;
    }

    public void ClearKitchenObject()
    {
        kitchenObject = null;
    }

    public void SetKitchenObject(KitchenObject kitchenObject)
    {
        this.kitchenObject = kitchenObject;
    }

    public bool HasKitchenObject()
    {
        return kitchenObject != null;
    }
    public KitchenObject GetKitchenObject()
    {
        return kitchenObject;
    }
}
