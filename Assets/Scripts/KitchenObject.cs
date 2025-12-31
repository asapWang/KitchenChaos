using UnityEngine;

public class KitchenObject : MonoBehaviour
{
    [SerializeField] private KitchenObjectSO kitchenObjectSO;
    
    private IGetKitchenObject iKitchenObjectParent;

    public KitchenObjectSO GetKitchenObjectSO()
    {
        return kitchenObjectSO;
    }
    public void SetOwner(IGetKitchenObject iKitchenObjectParent)
    {
        if(this.iKitchenObjectParent!=null)
        {
            this.iKitchenObjectParent.ClearKitchenObject();
        }
        this.iKitchenObjectParent = iKitchenObjectParent;
        this.iKitchenObjectParent.SetKitchenObject(this);
        transform.parent = iKitchenObjectParent.GetKitchenObjectPosition();
        transform.localPosition = Vector3.zero;
        
    }
    public static KitchenObject SpawnKitchenObject(KitchenObjectSO kitchenObjectSO, IGetKitchenObject iKitchenObjectParent)
    {
        //Instantiate会生成实例，并把第一个transform变成第二个transform的子物体，返回值是第一个transform，也可以不指定父对象
        Transform kitchenObjectTransform = Instantiate(kitchenObjectSO.kitchenObjectPrefab.transform);
        KitchenObject kitchenObject = kitchenObjectTransform.GetComponent<KitchenObject>();
        kitchenObject.SetOwner(iKitchenObjectParent);
        return kitchenObject;
    }

    public void DestroySelf()
    {
        iKitchenObjectParent.ClearKitchenObject(); 
        Destroy(gameObject);
    }


}
