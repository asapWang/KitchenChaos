using UnityEngine;
using UnityEngine.UI;

public class IconTemplate : MonoBehaviour
{
    //设置具体图标
    [SerializeField] private Image iconImage;
    public void SetIcon(KitchenObjectSO kitchenObjectSO)
    {
        iconImage.sprite = kitchenObjectSO.kitchenObjectSprite;
    }
}
