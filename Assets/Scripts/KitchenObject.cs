using Unity.Netcode;
using UnityEngine;

public class KitchenObject : NetworkBehaviour
{
    [SerializeField] private KitchenObjectSO kitchenObjectSO;
    
    private IGetKitchenObject iKitchenObjectParent;
    private FollowTransform followTransform;
    protected virtual void Awake()
    {
        followTransform = GetComponent<FollowTransform>();
    }

    public KitchenObjectSO GetKitchenObjectSO()
    {
        return kitchenObjectSO;
    }

    //设置KitchenObject的父对象，并更新位置和旋转
    public void SetOwner(IGetKitchenObject iKitchenObjectParent)
    {
        SetOwnerServerRpc(iKitchenObjectParent.GetNetworkObject());
    }
    [ServerRpc(RequireOwnership = false)]
    public void SetOwnerServerRpc(NetworkObjectReference iKitchenObjectParentNetworkObjectReference)
    {
        SetOwnerClientRpc(iKitchenObjectParentNetworkObjectReference);
    }
    [ClientRpc]
    public void SetOwnerClientRpc(NetworkObjectReference iKitchenObjectParentNetworkObjectReference)
    {
        iKitchenObjectParentNetworkObjectReference.TryGet(out NetworkObject iKitchenObjectParentNetworkObject);
        IGetKitchenObject iKitchenObjectParent = iKitchenObjectParentNetworkObject.GetComponent<IGetKitchenObject>();
        if(this.iKitchenObjectParent!=null)
        {
            this.iKitchenObjectParent.ClearKitchenObject();
        }
        this.iKitchenObjectParent = iKitchenObjectParent;
        this.iKitchenObjectParent.SetKitchenObject(this);
        followTransform.SetTargetTransform(this.iKitchenObjectParent.GetKitchenObjectPosition());
    }
    
    //这个方法是静态的，方便其他脚本调用来生成KitchenObject实例
    public static void SpawnKitchenObject(KitchenObjectSO kitchenObjectSO, IGetKitchenObject iKitchenObjectParent)
    {
        GameMultiplayer.Instance.SpawnKitchenObject(kitchenObjectSO, iKitchenObjectParent);
    }

    public void DestroySelf()
    {
        iKitchenObjectParent.ClearKitchenObject(); 
        Destroy(gameObject);
    }

    public bool TryGetPlate(out PlateKitchenObject plateKitchenObject)
    {
        if(this is PlateKitchenObject)
        {
            plateKitchenObject = this as PlateKitchenObject;
            return true;
        }else{
            plateKitchenObject = null;
            return false;
        }
    }


}
