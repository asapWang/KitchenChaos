using UnityEngine;
using System.Collections.Generic;
using System;

public class DeliveryManager : MonoBehaviour
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
    private float waitingRecipeTimerMax = 4f;
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
        waitingRecipeTimer += Time.deltaTime;
        if (waitingRecipeTimer >= waitingRecipeTimerMax)
        {
            //每隔4秒生成一个新的订单
            waitingRecipeTimer = 0f;
            if (waitingRecipeCount < waitingRecipeCountMax)
            {
                //最多只能有4个订单
                waitingRecipeCount++;
                waitingRecipeSOList.Add(recipeSOList.recipeSOs[UnityEngine.Random.Range(0, recipeSOList.recipeSOs.Length)]);
                OnRecipeSpawned?.Invoke(this, EventArgs.Empty);
                Debug.Log(waitingRecipeSOList[waitingRecipeCount - 1].recipeName);
            }
        }
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
                    Debug.Log("Recipe delivered");
                    amount++;
                    waitingRecipeSOList.RemoveAt(i);
                    waitingRecipeCount--;
                    OnRecipeCompleted?.Invoke(this, EventArgs.Empty);
                    OnDeliverSuccess?.Invoke(this, EventArgs.Empty);
                    return;
                }
            }   
        }
        //汉堡匹配失败
        OnDeliverFail?.Invoke(this, EventArgs.Empty);
        Debug.Log("Recipe not delivered");
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