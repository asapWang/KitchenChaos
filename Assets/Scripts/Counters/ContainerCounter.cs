using UnityEngine;
using System;

public class ContainerCounter : BaseCounter
{
    [SerializeField] private KitchenObjectSO kitchenObjectSO;
    public event EventHandler OnOpenContainer;
    
    public override void Interact(Player player)
    {
        if (!player.HasKitchenObject())
        {
            
            KitchenObject.SpawnKitchenObject(kitchenObjectSO, player);
            OnOpenContainer?.Invoke(this, EventArgs.Empty);
        }
       
    }
}
