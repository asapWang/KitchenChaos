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
                //Player有物体,判断是否为盘子
                if(player.GetKitchenObject().TryGetPlate(out PlateKitchenObject plateKitchenObject))
                {
                    //Player手上是盘子
                    if (plateKitchenObject.TryAddIngredient(GetKitchenObject().GetKitchenObjectSO()))
                    {
                        GetKitchenObject().DestroySelf();
                    }
                }else{
                    //Player手上不是盘子
                    if(GetKitchenObject().TryGetPlate(out plateKitchenObject))
                    {
                    //Counter上是盘子
                        if (plateKitchenObject.TryAddIngredient(player.GetKitchenObject().GetKitchenObjectSO()))
                        {
                            player.GetKitchenObject().DestroySelf();
                        }
                    }
                }
            }
            else
            {
                //Player没有物体
                GetKitchenObject().SetOwner(player);
            }
        }
    }
}