using UnityEngine;

public class BaseCounter : MonoBehaviour,IGetKitchenObject
{
    [SerializeField] private Transform topClearCounterPosition;
    private KitchenObject kitchenObject;
    public virtual void Interact(Player player)
    {
        Debug.Log("BaseCounter Interact");
    }
    public virtual void InteractAlternative()
    {
        Debug.Log("BaseCounter InteractAlternative");
    }






    //以下为接口实现
    public Transform GetKitchenObjectPosition()
    {
        return topClearCounterPosition;
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
