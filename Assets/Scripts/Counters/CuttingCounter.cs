using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class CuttingCounter : BaseCounter, IHasProgress
{
    [SerializeField] private CuttingRecipeSO[] cuttingRecipeSOArray;
    public event EventHandler OnCutting;
    public event EventHandler<IHasProgress.OnProgressBarUIChangedEventArgs> OnProgressBarUIChanged;
    //音效事件
    public static event EventHandler OnAnyCutting;
    private int cuttingProgress;
    //清空OnAnyCutting事件
    new public static void ResetStaticData()
    {
        OnAnyCutting = null;
    }
    public override void Interact(Player player)
    {
        if (!HasKitchenObject())
        {
            //Counter上没有物体
            if (player.HasKitchenObject())
            {
                //Player有物体
                if(HasRecipeWithInput(player.GetKitchenObject().GetKitchenObjectSO()))
                {
                    cuttingProgress = 0;
                    OnProgressBarUIChanged?.Invoke(this, new IHasProgress.OnProgressBarUIChangedEventArgs
                    {
                        progressNormalized = (float)cuttingProgress / GetCuttingRecipeSO(player.GetKitchenObject().GetKitchenObjectSO()).cuttingProgressMax
                    });
                    player.GetKitchenObject().SetOwner(this);
                }
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
                if(player.GetKitchenObject().TryGetPlate(out PlateKitchenObject plateKitchenObject))
                {
                    //Player手上是盘子
                    if (plateKitchenObject.TryAddIngredient(GetKitchenObject().GetKitchenObjectSO()))
                    {
                        GetKitchenObject().DestroySelf();
                    }
                }
            }
            else
            {
                //Player没有物体
                OnProgressBarUIChanged?.Invoke(this, new IHasProgress.OnProgressBarUIChangedEventArgs
                {
                     progressNormalized = 0f
                });
                GetKitchenObject().SetOwner(player);
            }
        }
    }

    public override void InteractAlternative()
    {
        if(HasKitchenObject() && HasRecipeWithInput(GetKitchenObject().GetKitchenObjectSO()))
        {
            cuttingProgress++;
            OnProgressBarUIChanged?.Invoke(this, new IHasProgress.OnProgressBarUIChangedEventArgs
            {
                 progressNormalized = (float)cuttingProgress / GetCuttingRecipeSO(GetKitchenObject().GetKitchenObjectSO()).cuttingProgressMax
            });
            OnCutting?.Invoke(this, EventArgs.Empty);
            OnAnyCutting?.Invoke(this, EventArgs.Empty);
            if(cuttingProgress>= GetCuttingRecipeSO(GetKitchenObject().GetKitchenObjectSO()).cuttingProgressMax)
            {
                KitchenObjectSO output= GetOutputCuttingRecipeSO(GetKitchenObject().GetKitchenObjectSO());
                GetKitchenObject().DestroySelf();
                KitchenObject.SpawnKitchenObject(output,this);
            }
        }
    }

    private KitchenObjectSO GetOutputCuttingRecipeSO(KitchenObjectSO input)
    {
        CuttingRecipeSO cuttingRecipeSO = GetCuttingRecipeSO(input);
        return cuttingRecipeSO.output;
    }

    private bool HasRecipeWithInput(KitchenObjectSO input)
    {
        CuttingRecipeSO cuttingRecipeSO = GetCuttingRecipeSO(input);
        return cuttingRecipeSO != null;
    }

    private CuttingRecipeSO GetCuttingRecipeSO(KitchenObjectSO input)
    {
        foreach (CuttingRecipeSO cuttingRecipeSO in cuttingRecipeSOArray)
        {
            if(cuttingRecipeSO.input==input)
            {
                return cuttingRecipeSO;
            }
        }
        return null;
    }

}
