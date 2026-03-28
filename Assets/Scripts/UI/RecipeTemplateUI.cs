using UnityEngine;
using UnityEngine.UI;

public class RecipeTemplateUI : MonoBehaviour
{
    [SerializeField] Transform container;
    [SerializeField] Transform imgIconTemplate;
    [SerializeField] Transform recipeName;
    //依旧是将图标模板隐藏起来，等到生成订单UI时再根据这个模板生成图标，隐藏起来的模版不会占用UI空间
    private void Awake() {
        imgIconTemplate.gameObject.SetActive(false);
    }

    public void SetRecipe(RecipeSO recipeSO)
    {
        recipeName.GetComponent<Text>().text = recipeSO.recipeName;
        foreach(KitchenObjectSO kitchenObjectSO in recipeSO.kitchenObjectSOs){
            Transform imgIconTransform = Instantiate(imgIconTemplate, container);
            imgIconTransform.GetComponent<Image>().sprite = kitchenObjectSO.kitchenObjectSprite;
            imgIconTransform.gameObject.SetActive(true);
        }
    }
}
