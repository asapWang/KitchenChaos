using System.Collections.Generic;
using UnityEngine;
using System;
using Unity.Netcode;

public class PlateKitchenObject : KitchenObject
{

    [SerializeField] private List<KitchenObjectSO> validKitchenObjectSOs;
    private List<KitchenObjectSO> addedKitchenObjectSOs;
    public event EventHandler<OnIngredientVisualShowedEventArgs> OnIngredientVisualShowed;
    public class OnIngredientVisualShowedEventArgs : EventArgs
    {
        public KitchenObjectSO kitchenObjectSO;
    }
    protected override void Awake()
    {
        base.Awake();
        addedKitchenObjectSOs = new List<KitchenObjectSO>();
    }

    public bool TryAddIngredient(KitchenObjectSO kitchenObjectSO)
    {
        if (!validKitchenObjectSOs.Contains(kitchenObjectSO))
        {
            return false;
        }
        if (addedKitchenObjectSOs.Contains(kitchenObjectSO))
        {
            return false;
        }
        AddIngredientServerRpc(GameMultiplayer.Instance.GetKitchenObjectSOIndex(kitchenObjectSO));
        return true;
    }
    //同步拿盘子装上物体
    [ServerRpc(RequireOwnership = false)]
    public void AddIngredientServerRpc(int kitchenObjectSOIndex)
    {
        AddIngredientClientRpc(kitchenObjectSOIndex);      
    }
    [ClientRpc]
    public void AddIngredientClientRpc(int kitchenObjectSOIndex)
    {
        KitchenObjectSO kitchenObjectSO = GameMultiplayer.Instance.GetKitchenObjectSOFromIndex(kitchenObjectSOIndex);
        addedKitchenObjectSOs.Add(kitchenObjectSO);
        OnIngredientVisualShowed?.Invoke(this, new OnIngredientVisualShowedEventArgs { kitchenObjectSO = kitchenObjectSO });
    }
    public List<KitchenObjectSO> GetAddedKitchenObjectSOs()
    {
        return addedKitchenObjectSOs;
    }
}
