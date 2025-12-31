using UnityEngine;
using UnityEngine.PlayerLoop;

public class ClearCounter : BaseCounter
{
    public override void Interact(Player player)
    {
        if (!HasKitchenObject())
        {
            //Counter上没有物体
            if (player.HasKitchenObject())
            {
                //Player有物体
                player.GetKitchenObject().SetOwner(this);
            }
            else
            {
                //Player没有物体
                //啥都不做
            }
        }
        else
        {
            //Counter上有物体
            if (player.HasKitchenObject())
            {
                //Player有物体
                //啥都不做
            }
            else
            {
                //Player没有物体
                GetKitchenObject().SetOwner(player);
            }
        }
    }


   

}
