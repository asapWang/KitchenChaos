using UnityEngine;
using System;
using Unity.Netcode;
using UnityEngine.SocialPlatforms;
using Unity.VisualScripting;
using System.Collections.Generic;

public class Player : NetworkBehaviour, IGetKitchenObject
{
    [SerializeField] private float moveSpeed;
    [SerializeField] private LayerMask counterLayerMask;
    [SerializeField] private LayerMask collisionLayerMask;
    [SerializeField] private Transform playerHandPosition;
    [SerializeField] private List<Vector3> spawnPositionList;
    //isMoving只用于动画控制
    private bool isMoving;
    private Vector3 lastMoveDir;
    private BaseCounter selectedCounter;
    private KitchenObject kitchenObject;
    //突出SelectedCounterVisual事件
    public event EventHandler<SelectedCounterEventArgs> OnSelectedCounter;
    public class SelectedCounterEventArgs : EventArgs
    {
        public BaseCounter selectedCounter;
    }
    //拾取物品音效事件
    public event EventHandler OnPickup;
    public static event EventHandler OnPlayerSpawned;
    public static event EventHandler OnAnyPlayerPickup;
    public static Player LocalInstance { get; private set; }
    
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            LocalInstance = this;
            OnPlayerSpawned?.Invoke(this, EventArgs.Empty);
        }
        transform.position = spawnPositionList[(int)OwnerClientId];
        //玩家退出游戏，如果玩家手上有物品，则销毁物品
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_OnClientDisconnectCallback;
        }
    }
    private void NetworkManager_OnClientDisconnectCallback(ulong clientId)
    {
         // 整个服务器正在关闭，不需要逐个清理玩家物品
        if (NetworkManager.Singleton == null ||
            NetworkManager.Singleton.ShutdownInProgress)
        {   
            return;
        }

        // 只有普通Client掉线时，才清理他遗留且仍Spawn的物品
        if (clientId == OwnerClientId && HasKitchenObject())
        {
            KitchenObject.DestroyKitchenObject(GetKitchenObject());
        }
    }
    private void Start()
    {
        InputSystem.Instance.OnInteractAction += InputSystem_OnInteractAction;
        InputSystem.Instance.OnInteractAlternativeAction += InputSystem_OnInteractAlternativeAction;
    }

    private void InputSystem_OnInteractAction(object sender, System.EventArgs e)
    {
        //游戏状态不是Playing，不能交互
        if (!GameManager.Instance.IsPlaying())
        {
            return;
        }
        if (selectedCounter != null)
        {
            selectedCounter.Interact(this);
        }
    }
    private void InputSystem_OnInteractAlternativeAction(object sender, System.EventArgs e)
    {
        if (!GameManager.Instance.IsPlaying())
        {
            return;
        }
        if (selectedCounter != null)
        {
            selectedCounter.InteractAlternative();
        }
    }
    
   
    private void Update()
    {
        //IsOwner是NetworkBehaviour提供的属性，用于判断当前脚本实例是否属于本地玩家。只有本地玩家应该处理输入和控制角色移动，其他玩家的实例应该忽略这些操作。
        if (!IsOwner){
            return;
        }
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
        Vector2 movePosition = InputSystem.Instance.GetMovementInput();
        Vector3 realMovePosition = new Vector3(movePosition.x, 0, movePosition.y);
        float rotationSpeed = 10f;
        transform.forward = Vector3.Slerp(transform.forward, realMovePosition, Time.deltaTime * rotationSpeed);
        //碰撞检测
        float playerHeight = 2f;
        float playerRadius = 0.7f;
        float moveDistance = moveSpeed * Time.deltaTime;
        bool canMove= !Physics.BoxCast(transform.position, Vector3.one * playerRadius, realMovePosition, Quaternion.identity, moveDistance, collisionLayerMask);
        if (!canMove)
        {
            //加上0.5f的容错，是因为手柄不可能完全水平或者垂直，eg:玩家用手柄只想对准上方柜子，摇杆不必完全垂直向上便能停下；
            //0.5的容错不是死定的，可以自己调节，但别搞错向量1的45度分量不是0.5，而是0.7071，所以0.5的容错其实是比较大的了，玩家只要摇杆大致朝一个方向，就能停下来，这样手柄操作就不会太麻烦了
            //改为boxcast，避免胶囊体碰到box边缘时，x和z方向都不能移动，导致玩家卡住
            canMove = (realMovePosition.x<-0.5f||realMovePosition.x>0.5f) && !Physics.BoxCast(transform.position, Vector3.one * playerRadius, new Vector3(realMovePosition.x, 0, 0), Quaternion.identity, moveDistance, collisionLayerMask);
            if (canMove)
            {
                realMovePosition = new Vector3(realMovePosition.x, 0, 0).normalized;
            }
            else
            {
                canMove = (realMovePosition.z<-0.5f||realMovePosition.z>0.5f) && !Physics.BoxCast(transform.position, Vector3.one * playerRadius, new Vector3(0, 0, realMovePosition.z), Quaternion.identity, moveDistance, collisionLayerMask);
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
        Vector2 movePosition = InputSystem.Instance.GetMovementInput();
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

    public static void ResetStaticData()
    {
        OnPlayerSpawned = null;
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
        if (kitchenObject != null)
        {
            OnPickup?.Invoke(this, EventArgs.Empty);
            OnAnyPlayerPickup?.Invoke(this, EventArgs.Empty);
        }
    }

    public bool HasKitchenObject()
    {
        return kitchenObject != null;
    }
    public KitchenObject GetKitchenObject()
    {
        return kitchenObject;
    }
    public NetworkObject GetNetworkObject()
    {
        return NetworkObject;
    }
}
