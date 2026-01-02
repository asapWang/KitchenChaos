using System.Collections.Generic;
using UnityEngine;

public class PlateKitchenObject : KitchenObject
{
    [SerializeField] private List<KitchenObjectSO> validKitchenObjectSOs;
    private List<KitchenObjectSO> addedKitchenObjectSOs;
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
        return true;
    }
}
