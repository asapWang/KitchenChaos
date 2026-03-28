using UnityEngine;

public class PlateIconsUI : MonoBehaviour
{
    [SerializeField] private PlateKitchenObject plateKitchenObject;
    [SerializeField] private Transform iconTemplate;
    private void Awake()
    {
        iconTemplate.gameObject.SetActive(false);
    }
    private void Start()
    {
        plateKitchenObject.OnIngredientVisualShowed += PlateKitchenObject_OnIngredientIconShowed;
    }
    private void PlateKitchenObject_OnIngredientIconShowed(object sender, PlateKitchenObject.OnIngredientVisualShowedEventArgs e)
    {
        UpdateIcons();
    }
    //更新图标显示，采用方法是先删除之前所有的图标，再根据盘子里已有的食材生成新的图标
    public void UpdateIcons()
    {
        foreach(Transform child in transform)
        {
            //每次更新不能把模板删了
            if(child==iconTemplate) 
                continue;
            Destroy(child.gameObject);
        }
        foreach(KitchenObjectSO kitchenObjectSO in plateKitchenObject.GetAddedKitchenObjectSOs())
        {
            Transform iconTransform = Instantiate(iconTemplate, transform);
            //注意这里SetActive的是新生成的图标，因为复刻的模板而模板本身是不显示的
            iconTransform.gameObject.SetActive(true);
            iconTransform.GetComponent<IconTemplate>().SetIcon(kitchenObjectSO);
        }
    }
}
