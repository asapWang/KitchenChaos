using UnityEngine;
using System.Collections.Generic;
using System;
using Unity.Netcode;

public class DeliveryManager : NetworkBehaviour
{
    [SerializeField] private RecipeSOList recipeSOList;
    //事件：生成订单和完成订单时都要更新订单UI
    public event EventHandler OnRecipeSpawned;
    public event EventHandler OnRecipeCompleted;
    //事件:当订单被送达时，播放送达成功的音效；当订单错误时，播放送达失败的音效
    public event EventHandler OnDeliverFail;
    public event EventHandler OnDeliverSuccess;

    public static DeliveryManager Instance { get; private set; }
    private List<RecipeSO> waitingRecipeSOList;
    private float waitingRecipeTimer;
    private float waitingRecipeTimerMax = 8f;
    private int waitingRecipeCount;
    private int waitingRecipeCountMax = 4;
    //成功送达订单的数量
    private int amount;
    private void Awake()
    {
        Instance = this;
        waitingRecipeSOList = new List<RecipeSO>();
    }
    private void Update()
    {
        if (!IsServer) 
            return;
        waitingRecipeTimer += Time.deltaTime;
        //加个条件，只有在游戏状态是playing的时候才生成订单
        if (waitingRecipeTimer >= waitingRecipeTimerMax && GameManager.Instance.IsPlaying())
        {
            //每隔8秒生成一个新的订单
            waitingRecipeTimer = 0f;
            if (waitingRecipeCount < waitingRecipeCountMax)
            {
                //Rpc函数只能值类型，不能传递引用类型，所以这里传递订单SO的索引来生成订单
                int waitingRecipeSOIndex = UnityEngine.Random.Range(0, recipeSOList.recipeSOs.Length);
                //根据索引得到订单SO，并添加到等待订单列表中
                SpawnRecipeClientRpc(waitingRecipeSOIndex);

            }
        }
    }
    //生成订单的ClientRpc函数，参数是订单SO的索引
    [ClientRpc]
    private void SpawnRecipeClientRpc(int waitingRecipeSOIndex)
    {
        waitingRecipeCount++;
        waitingRecipeSOList.Add(recipeSOList.recipeSOs[waitingRecipeSOIndex]);
        OnRecipeSpawned?.Invoke(this, EventArgs.Empty);
        Debug.Log(waitingRecipeSOList[waitingRecipeCount - 1].recipeName);
    }
    public void DeliverRecipe(PlateKitchenObject plateKitchenObject)
    {
        for(int i = 0; i < waitingRecipeSOList.Count; i++){
            //先判断盘子里的料理数量和订单[i]里的料理数量是否相等，如果不相等就直接去匹配下一个订单[i+1]，这样也能避免订单[i]真包含盘子里料理的情况
            if(waitingRecipeSOList[i].kitchenObjectSOs.Length == plateKitchenObject.GetAddedKitchenObjectSOs().Count){
                //大致思路：盘子里的每个料理都去找订单[i]里的每个料理，有不匹配的就将isContentCorrect设为false
                bool isContentCorrect = true;
                foreach(KitchenObjectSO kitchenObjectSO in plateKitchenObject.GetAddedKitchenObjectSOs()){
                    //isIngredientCorrect用来判断盘子里的每个料理是否能与订单[i]里的料理匹配
                    bool isIngredientCorrect = false;
                    foreach(KitchenObjectSO recipeKitchenObjectSO in waitingRecipeSOList[i].kitchenObjectSOs){
                        if(kitchenObjectSO == recipeKitchenObjectSO){
                            //匹配成功了就直接跳出循环，去匹配盘子里的下一个料理
                            isIngredientCorrect = true;
                            break;
                        }
                    }
                    //只要有一个料理没有匹配成功，就说明不符合这个订单[i]，isContentCorrect就直接等于false
                    if(!isIngredientCorrect){
                        isContentCorrect = false;
                        break;
                    }
                }
                //每个料理匹配结束后，如果isContentCorrect还是true，说明汉堡匹配成功，跳出函数
                if(isContentCorrect){
                    DeliverRecipeSuccessServerRpc(i);
                    return;
                }
            }   
        }
        //汉堡匹配失败
        DeliverRecipeFailServerRpc();
    }
    //客户端检验订单提交成功后调用ServerRpc函数，再由ServerRpc函数调用ClientRpc函数来更新所有客户端的订单状态
    [ServerRpc(InvokePermission = RpcInvokePermission.Everyone)]
    private void DeliverRecipeSuccessServerRpc(int deliveredRecipeIndex)
    {
        DeliverRecipeSuccessClientRpc(deliveredRecipeIndex);
    }
    [ClientRpc]
    private void DeliverRecipeSuccessClientRpc(int deliveredRecipeIndex)
    {
        Debug.Log("Recipe delivered");
        amount++;
        waitingRecipeSOList.RemoveAt(deliveredRecipeIndex);
        waitingRecipeCount--;
        OnRecipeCompleted?.Invoke(this, EventArgs.Empty);
        OnDeliverSuccess?.Invoke(this, EventArgs.Empty);
    }
    //同理-订单提交失败
    [ServerRpc(InvokePermission = RpcInvokePermission.Everyone)]
    public void DeliverRecipeFailServerRpc()
    {
        DeliverRecipeFailClientRpc();
    }
    [ClientRpc]
    private void DeliverRecipeFailClientRpc()
    {
        Debug.Log("Recipe delivery failed");
        OnDeliverFail?.Invoke(this, EventArgs.Empty);
    }

    //获取当前等待订单的SO列表，方便根据这个列表来生成订单UI
    public List<RecipeSO> GetWaitingRecipeSOList()
    {
        return waitingRecipeSOList;
    }
    //获取成功送达订单的数量
    public int GetAmount()
    {
        return amount;
    }
}