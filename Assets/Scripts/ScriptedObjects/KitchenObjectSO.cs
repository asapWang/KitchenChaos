using UnityEngine;

[CreateAssetMenu(fileName = "KitchenObjectSO", menuName = "Scriptable Objects/KitchenObjectSO")]
public class KitchenObjectSO : ScriptableObject
{
    public string kitchenObjectName;
    public Sprite kitchenObjectSprite;
    public GameObject kitchenObjectPrefab;
    
}
