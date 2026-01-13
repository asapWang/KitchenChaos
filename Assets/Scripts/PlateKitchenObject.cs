using System.Collections.Generic;
using UnityEngine;
using System;

public class PlateKitchenObject : KitchenObject
{

    [SerializeField] private List<KitchenObjectSO> validKitchenObjectSOs;
    private List<KitchenObjectSO> addedKitchenObjectSOs;
    public event EventHandler<OnIngredientVisualShowedEventArgs> OnIngredientVisualShowed;
    public class OnIngredientVisualShowedEventArgs : EventArgs
    {
        public KitchenObjectSO kitchenObjectSO;
    }
    private void Awake()
    {
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
        addedKitchenObjectSOs.Add(kitchenObjectSO);
        OnIngredientVisualShowed?.Invoke(this, new OnIngredientVisualShowedEventArgs { kitchenObjectSO = kitchenObjectSO });
        return true;
    }
}
