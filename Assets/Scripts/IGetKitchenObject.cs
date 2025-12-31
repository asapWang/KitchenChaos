using UnityEngine;

public interface IGetKitchenObject
{
    public Transform GetKitchenObjectPosition();
 
    public void ClearKitchenObject();

    public void SetKitchenObject(KitchenObject kitchenObject);

    public bool HasKitchenObject();

    public KitchenObject GetKitchenObject();
   
}
