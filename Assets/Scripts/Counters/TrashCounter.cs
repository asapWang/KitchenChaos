using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class TrashCounter : BaseCounter
{
    //扔垃圾音效事件
    public static event EventHandler OnAnyObjectThrownHere;
    public override void Interact(Player player)
    {
        if (player.HasKitchenObject())
        {
            player.GetKitchenObject().DestroySelf();
            OnAnyObjectThrownHere?.Invoke(this, EventArgs.Empty);
        }
    }
}
