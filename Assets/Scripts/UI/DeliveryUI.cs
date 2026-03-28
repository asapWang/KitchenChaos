using UnityEngine;

public class DeliveryUI : MonoBehaviour
{
    [SerializeField] private Transform container;
    [SerializeField] private Transform recipeTemplate;
    //一开始就把订单UI模板隐藏起来，等到生成订单时再根据这个模板生成订单UI，隐藏起来的模版不会占用UI空间
    private void Awake() {
        recipeTemplate.gameObject.SetActive(false);
    }
    //在Start函数里订阅DeliveryManager的两个事件，这样每当生成订单或者完成订单时，订单UI都会更新显示
    private void Start() {
        DeliveryManager.Instance.OnRecipeSpawned += DeliveryManager_OnRecipeSpawned;
        DeliveryManager.Instance.OnRecipeCompleted += DeliveryManager_OnRecipeCompleted;
    }

    private void DeliveryManager_OnRecipeSpawned(object sender, System.EventArgs e) {
        UpdateVisual();
    }

    private void DeliveryManager_OnRecipeCompleted(object sender, System.EventArgs e) {
        UpdateVisual();
    }

    //更新订单UI的显示，无论是生成订单还是完成订单都调用这个函数，先销毁原来所有的订单UI，再根据当前的订单列表生成新的订单UI
    private void UpdateVisual()
    {
        //先销毁原来所有的订单UI,注意不能销毁recipeTemplate这个模板
        foreach(Transform child in container){
            if(child == recipeTemplate) continue;
            Destroy(child.gameObject);
        }
        foreach(RecipeSO recipeSO in DeliveryManager.Instance.GetWaitingRecipeSOList()){
            Transform recipeTransform = Instantiate(recipeTemplate, container);
            recipeTransform.gameObject.SetActive(true);
            //每个订单UI都调用SetRecipe函数来设置订单UI的显示内容，传入订单SO作为参数
            recipeTransform.GetComponent<RecipeTemplateUI>().SetRecipe(recipeSO);
        }
    }
}
