using System;
using UnityEngine;
using System.Collections.Generic;

public class PlateCompleteVisual : MonoBehaviour
{
    [Serializable]
    public struct Ingredient
    {
        public KitchenObjectSO kitchenObjectSO;
        public GameObject gameObject;
    }
    [SerializeField] private PlateKitchenObject plateKitchenObject;
    [SerializeField] private List<Ingredient> ingredients; 
    private void Start()
    {
        plateKitchenObject.OnIngredientVisualShowed += PlateKitchenObject_OnIngredientVisualShowed;
    }
    private void PlateKitchenObject_OnIngredientVisualShowed(object sender, PlateKitchenObject.OnIngredientVisualShowedEventArgs e)
    {
        foreach(Ingredient ingredient in ingredients)
        {
            if(ingredient.kitchenObjectSO == e.kitchenObjectSO)
            {
                ingredient.gameObject.SetActive(true);
            }
        }
    }
}
