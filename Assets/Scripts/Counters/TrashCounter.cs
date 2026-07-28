using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Unity.Netcode;

public class TrashCounter : BaseCounter
{
    //扔垃圾音效事件
    public static event EventHandler OnAnyObjectThrownHere;
    //清空OnAnyObjectThrownHere事件
    new public static void ResetStaticData()
    {
        OnAnyObjectThrownHere = null;
    }
    public override void Interact(Player player)
    {
        if (player.HasKitchenObject())
        {
            KitchenObject.DestroyKitchenObject(player.GetKitchenObject());
            InteractLogicServerRpc();
        }
    }
    //同步扔垃圾音效
    [ServerRpc(RequireOwnership = false)]
    public void InteractLogicServerRpc()
    {
        InteractLogicClientRpc();
    }
    [ClientRpc]
    public void InteractLogicClientRpc()
    {
        OnAnyObjectThrownHere?.Invoke(this, EventArgs.Empty);
    }
}
