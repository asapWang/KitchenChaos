using System;
using UnityEngine;

public class StoveCounter : BaseCounter,IHasProgress
{
    [SerializeField] private FryingRecipeSO[] fryingRecipeSOArray;
    [SerializeField] private BurningRecipeSO[] burningRecipeSOArray;
    private FryingRecipeSO fryingRecipeSO;
    private BurningRecipeSO burningRecipeSO;
    public event EventHandler<IHasProgress.OnProgressBarUIChangedEventArgs> OnProgressBarUIChanged;
    public event EventHandler<OnStoveVisualChangeEventArgs> OnStoveVisualChange;
    public class OnStoveVisualChangeEventArgs : EventArgs
    {
        public State state;
    }
    public enum State
    {
        Idle,
        Frying,
        Fried,
        Burning
    }
    private float fryingProgress;
    private float burningProgress;

    private State state;
    private void Start()
    {
        state=State.Idle;
    }
    private void Update()
    {
        switch(state)
        {
            case State.Idle:
                break;
            case State.Frying:
                fryingProgress+=Time.deltaTime;
                OnProgressBarUIChanged?.Invoke(this, new IHasProgress.OnProgressBarUIChangedEventArgs
                {
                    progressNormalized = fryingProgress / fryingRecipeSO.fryingProgressMax
                });
                if(fryingProgress>= fryingRecipeSO.fryingProgressMax)
                {
                    //Frying complete
                    KitchenObjectSO outputFryingRecipeSO = GetOutputFryingRecipeSO(GetKitchenObject().GetKitchenObjectSO());
                    GetKitchenObject().DestroySelf();
                    KitchenObject.SpawnKitchenObject(outputFryingRecipeSO, this);
                    burningRecipeSO = GetBurningRecipeSO(outputFryingRecipeSO);
                    burningProgress = 0f;
                    state = State.Fried;
                    OnStoveVisualChange?.Invoke(this, new OnStoveVisualChangeEventArgs { state = state } );
                }
                break;
            case State.Fried:
                burningProgress+=Time.deltaTime;
                OnProgressBarUIChanged?.Invoke(this, new IHasProgress.OnProgressBarUIChangedEventArgs
                {
                    progressNormalized = burningProgress / burningRecipeSO.burningProgressMax
                });
                if(burningProgress>= burningRecipeSO.burningProgressMax)
                {
                    //Burning complete
                    KitchenObjectSO outputBurningRecipeSO = GetOutputBurningRecipeSO(GetKitchenObject().GetKitchenObjectSO());
                    GetKitchenObject().DestroySelf();
                    KitchenObject.SpawnKitchenObject(outputBurningRecipeSO, this);
                    state = State.Burning;
                    OnStoveVisualChange?.Invoke(this, new OnStoveVisualChangeEventArgs { state = state } );
                }
                break;
            case State.Burning:
                break;
        }
        
    }
    public override void Interact(Player player)
    {
        if (!HasKitchenObject())
        {
            //Counter上没有物体
            if (player.HasKitchenObject())
            {
                //Player有物体
                if(HasFryingRecipeWithInput(player.GetKitchenObject().GetKitchenObjectSO()))
                {
                    player.GetKitchenObject().SetOwner(this);
                    fryingRecipeSO = GetFryingRecipeSO(GetKitchenObject().GetKitchenObjectSO());
                    fryingProgress = 0f;
                    state = State.Frying;
                    OnStoveVisualChange?.Invoke(this, new OnStoveVisualChangeEventArgs { state = state } );
                }
            }
            else
            {
                //Player没有物体
                //啥都不做
            }
        }
        else
        {
            //Counter上有物体
            if (player.HasKitchenObject())
            {
                //Player有物体
                //啥都不做
            }
            else
            {
                //Player没有物体
                GetKitchenObject().SetOwner(player);
                state = State.Idle;
                OnStoveVisualChange?.Invoke(this, new OnStoveVisualChangeEventArgs { state = state } );
                OnProgressBarUIChanged?.Invoke(this, new IHasProgress.OnProgressBarUIChangedEventArgs
                {
                    progressNormalized = 0f
                });
            }
        }
    }
    private bool HasFryingRecipeWithInput(KitchenObjectSO input)
    {
        FryingRecipeSO fryingRecipeSO = GetFryingRecipeSO(input);
        return fryingRecipeSO != null;
    }
    private KitchenObjectSO GetOutputFryingRecipeSO(KitchenObjectSO input)
    {
        FryingRecipeSO fryingRecipeSO = GetFryingRecipeSO(input);
        return fryingRecipeSO.output;
    }
    private KitchenObjectSO GetOutputBurningRecipeSO(KitchenObjectSO input)
    {
        BurningRecipeSO burningRecipeSO = GetBurningRecipeSO(input);
        return burningRecipeSO.output;
    }

    private FryingRecipeSO GetFryingRecipeSO(KitchenObjectSO input)
    {
        foreach (FryingRecipeSO fryingRecipeSO in fryingRecipeSOArray)
        {
            if(fryingRecipeSO.input==input)
            {
                return fryingRecipeSO;
            }
        }
        return null;
    }

    private BurningRecipeSO GetBurningRecipeSO(KitchenObjectSO input)
    {
        foreach (BurningRecipeSO burningRecipeSO in burningRecipeSOArray)
        {
            if(burningRecipeSO.input==input)
            {
                return burningRecipeSO;
            }
        }
        return null;
    }

}
