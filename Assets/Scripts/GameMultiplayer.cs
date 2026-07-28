using Unity.Netcode;
using UnityEngine;

public class GameMultiplayer : NetworkBehaviour
{
    [SerializeField] private KitchenObjectListSO kitchenObjectListSO;
    public static GameMultiplayer Instance { get; private set; }
    private void Awake()
    {
        Instance = this;
    }
    
    //KitchenObject脚本调用这个方法继而调用ServerRpc来生成KitchenObject实例并同步
    public void SpawnKitchenObject(KitchenObjectSO kitchenObjectSO, IGetKitchenObject iKitchenObjectParent)
    {
        //通过转换，把KitchenObjectSO转换成索引和把父物体转换为NetworkObject，传递给RPC方法
        SpawnKitchenObjectServerRpc(GetKitchenObjectSOIndex(kitchenObjectSO), iKitchenObjectParent.GetNetworkObject());
    }
    [ServerRpc(RequireOwnership = false)]
    //RPC方法的参数不能是引用类型，所以传递KitchenObjectSO的索引和结构体NetworkObjectReference，此结构体可以接受NetworkObject作为参数，并在RPC方法中通过TryGet方法获取NetworkObject
    public void SpawnKitchenObjectServerRpc(int kitchenObjectSOIndex, NetworkObjectReference ikitchenObjectParentNetworkObjectReference)
    {
        //Instantiate会生成实例，并把第一个transform变成第二个transform的子物体，返回值是第一个transform，也可以不指定父对象
        Transform kitchenObjectTransform = Instantiate(GetKitchenObjectSOFromIndex(kitchenObjectSOIndex).kitchenObjectPrefab.transform);
        //Spawn方法会在所有客户端生成实例
        KitchenObject kitchenObject = kitchenObjectTransform.GetComponent<KitchenObject>();
        kitchenObject.NetworkObject.Spawn();
        //通过NetworkObjectReference获取NetworkObject，再通过GetComponent获取IGetKitchenObject类型的父对象
        ikitchenObjectParentNetworkObjectReference.TryGet(out NetworkObject ikitchenObjectParentNetworkObject);
        IGetKitchenObject ikitchenObjectParent = ikitchenObjectParentNetworkObject.GetComponent<IGetKitchenObject>();
        kitchenObject.SetOwner(ikitchenObjectParent);
    }

    //KitchenObject脚本调用这个方法继而调用ServerRpc来销毁KitchenObject实例并同步
    public void DestroyKitchenObject(KitchenObject kitchenObject)
    {
        DestroyKitchenObjectServerRpc(kitchenObject.NetworkObject);
    }
    [ServerRpc(RequireOwnership = false)]
    public void DestroyKitchenObjectServerRpc(NetworkObjectReference kitchenObjectNetworkObjectReference)
    {
        kitchenObjectNetworkObjectReference.TryGet(out NetworkObject kitchenObjectNetworkObject);
        KitchenObject kitchenObject = kitchenObjectNetworkObject.GetComponent<KitchenObject>();
        //销毁KitchenObject实例前，先清楚父对象对kitchenObject的引用
        ClearKitchenObjectClientRpc(kitchenObject.NetworkObject);
        kitchenObject.DestroySelf();
    }
    //清楚父对象对kitchenObject的引用
    [ClientRpc]
    public void ClearKitchenObjectClientRpc(NetworkObjectReference kitchenObjectNetworkObjectReference)
    {
        kitchenObjectNetworkObjectReference.TryGet(out NetworkObject kitchenObjectNetworkObject);
        KitchenObject kitchenObject = kitchenObjectNetworkObject.GetComponent<KitchenObject>();
        kitchenObject.ClearKitchenObjectOnParent();
    }










    
    //得到KitchenObjectSO的索引
    private int GetKitchenObjectSOIndex(KitchenObjectSO kitchenObjectSO)
    {
        return kitchenObjectListSO.kitchenObjectSOList.IndexOf(kitchenObjectSO);
    }
    //根据索引得到KitchenObjectSO
    private KitchenObjectSO GetKitchenObjectSOFromIndex(int index)
    {
        return kitchenObjectListSO.kitchenObjectSOList[index];
    }
}
