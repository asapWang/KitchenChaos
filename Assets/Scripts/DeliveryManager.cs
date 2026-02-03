using UnityEngine;
using System.Collections.Generic;

public class DeliveryManager : MonoBehaviour
{
    [SerializeField] private RecipeSOList recipeSOList;
    public static DeliveryManager Instance { get; private set; }
    private List<RecipeSO> waitingRecipeSOList;
    private float waitingRecipeTimer;
    private float waitingRecipeTimerMax = 4f;
    private int waitingRecipeCount;
    private int waitingRecipeCountMax = 4;
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
            waitingRecipeTimer = 0f;
            if (waitingRecipeCount < waitingRecipeCountMax)
            {
                waitingRecipeCount++;
                waitingRecipeSOList.Add(recipeSOList.recipeSOs[Random.Range(0, recipeSOList.recipeSOs.Length)]);
                Debug.Log(waitingRecipeSOList[waitingRecipeCount - 1].recipeName);
            }
        }
    }
    public void DeliverRecipe(PlateKitchenObject plateKitchenObject)
    {
        for(int i = 0; i < waitingRecipeSOList.Count; i++){
            if(waitingRecipeSOList[i].kitchenObjectSOs.Length == plateKitchenObject.GetAddedKitchenObjectSOs().Count){
                bool isContentCorrect = true;
                foreach(KitchenObjectSO kitchenObjectSO in plateKitchenObject.GetAddedKitchenObjectSOs()){
                    bool isIngredientCorrect = false;
                    foreach(KitchenObjectSO recipeKitchenObjectSO in waitingRecipeSOList[i].kitchenObjectSOs){
                        if(kitchenObjectSO == recipeKitchenObjectSO){
                            isIngredientCorrect = true;
                            break;
                        }
                    }
                    if(!isIngredientCorrect){
                        isContentCorrect = false;
                    }
                }
                if(isContentCorrect){
                    Debug.Log("Recipe delivered");
                    waitingRecipeSOList.RemoveAt(i);
                    waitingRecipeCount--;
                    return;
                }
            }   
        }
    }
}