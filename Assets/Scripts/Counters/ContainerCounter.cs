using UnityEngine;
using System;
using Unity.Netcode;

public class ContainerCounter : BaseCounter
{
    [SerializeField] private KitchenObjectSO kitchenObjectSO;
    public event EventHandler OnOpenContainer;
    
    public override void Interact(Player player)
    {
        if (!player.HasKitchenObject())
        {
            
            KitchenObject.SpawnKitchenObject(kitchenObjectSO, player);
            InteractServerRpc();
        }
       
    }
    [ServerRpc(RequireOwnership = false)]
    public void InteractServerRpc()
    {
        InteractClientRpc();
    }
    [ClientRpc]
    public void InteractClientRpc()
    {
        OnOpenContainer?.Invoke(this, EventArgs.Empty);
    }
}
