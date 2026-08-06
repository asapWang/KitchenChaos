using System;
using Unity.Netcode;
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
                    InteractLogicServerRpc();
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
                        KitchenObject.DestroyKitchenObject(GetKitchenObject());
                    }
                }
            }
            else
            {
                //Player没有物体
                InteractLogicServerRpc();
                GetKitchenObject().SetOwner(player);
            }
        }
    }
    //同步玩家Interact柜台的逻辑，让所有客户端的进度条归零
    [ServerRpc(RequireOwnership = false)]
    public void InteractLogicServerRpc()
    {
        InteractLogicClientRpc();
    }
    [ClientRpc]
    public void InteractLogicClientRpc()
    {
        cuttingProgress = 0;
        OnProgressBarUIChanged?.Invoke(this, new IHasProgress.OnProgressBarUIChangedEventArgs
        {
            progressNormalized = 0f
        });
    }

    public override void InteractAlternative()
    {
        if(HasKitchenObject() && HasRecipeWithInput(GetKitchenObject().GetKitchenObjectSO()))
        {
            CutObjectServerRpc();
            TestingCuttingProgressDownServerRpc();
        }
    }
    //同步物体放到柜子上之后的切割
    [ServerRpc(RequireOwnership = false)]
    public void CutObjectServerRpc()
    {
        CutObjectClientRpc();
    }
    [ClientRpc]
    public void CutObjectClientRpc()
    {
        cuttingProgress++;
        OnProgressBarUIChanged?.Invoke(this, new IHasProgress.OnProgressBarUIChangedEventArgs
        {
                progressNormalized = (float)cuttingProgress / GetCuttingRecipeSO(GetKitchenObject().GetKitchenObjectSO()).cuttingProgressMax
        });
        OnCutting?.Invoke(this, EventArgs.Empty);
        OnAnyCutting?.Invoke(this, EventArgs.Empty);
    }
    //判断切割是否完成，让server销毁和生成物体
    [ServerRpc(RequireOwnership = false)]
    public void TestingCuttingProgressDownServerRpc()
    {
        if(cuttingProgress>= GetCuttingRecipeSO(GetKitchenObject().GetKitchenObjectSO()).cuttingProgressMax)
        {
            KitchenObjectSO output= GetOutputCuttingRecipeSO(GetKitchenObject().GetKitchenObjectSO());
            KitchenObject.DestroyKitchenObject(GetKitchenObject());
            KitchenObject.SpawnKitchenObject(output,this);
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
