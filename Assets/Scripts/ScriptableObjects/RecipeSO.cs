using UnityEngine;

[CreateAssetMenu(fileName = "RecipeSO", menuName = "Scriptable Objects/RecipeSO")]
public class RecipeSO : ScriptableObject
{
    public KitchenObjectSO[] kitchenObjectSOs;
    public string recipeName;
}
