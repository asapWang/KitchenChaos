using UnityEngine;
using System;
public class BaseCounter : MonoBehaviour,IGetKitchenObject
{
    [SerializeField] private Transform topClearCounterPosition;
    private KitchenObject kitchenObject;
    //音效事件
    public static event EventHandler OnAnyObjectPlacedHere;
    //清空OnAnyObjectPlacedHere事件
    public static void ResetStaticData()
    {
        OnAnyObjectPlacedHere = null;
    }
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
        //当有物体被放在Counter上时，触发音效事件
        if(kitchenObject!=null)
        {
            OnAnyObjectPlacedHere?.Invoke(this, EventArgs.Empty);
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
}
