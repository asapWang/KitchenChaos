using System;
using UnityEngine;
using Unity.Netcode;
using Unity.VisualScripting;

public class StoveCounter : BaseCounter,IHasProgress
{
    [SerializeField] private FryingRecipeSO[] fryingRecipeSOArray;
    [SerializeField] private BurningRecipeSO[] burningRecipeSOArray;
    private FryingRecipeSO fryingRecipeSO;
    private BurningRecipeSO burningRecipeSO;
    //管进度条的
    public event EventHandler<IHasProgress.OnProgressBarUIChangedEventArgs> OnProgressBarUIChanged;
    //管声音和特效的
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
    //使用NetworkVariable同步时间与状态
    private NetworkVariable<float> fryingProgress = new NetworkVariable<float>(0f);
    private NetworkVariable<float> burningProgress = new NetworkVariable<float>(0f);
    private NetworkVariable<State> state= new NetworkVariable<State>(State.Idle);
    //NetworkVariable的值在Server上修改，客户端会自动同步，而且NetworkVariable还有OnValueChanged事件可以监听值的变化
    //由于这是NetworkVariable，所以要在OnNetworkSpawn里注册事件，而不是在Start里注册事件
    public override void OnNetworkSpawn()
    {
        fryingProgress.OnValueChanged += FryingProgress_OnValueChanged;
        burningProgress.OnValueChanged += BurningProgress_OnValueChanged;
        state.OnValueChanged += State_OnValueChanged;
    } 
    private void FryingProgress_OnValueChanged(float previousValue, float newValue)
    {
        //使用三元运算符来避免fryingRecipeSO为null的情况，防止报错
        //不同客户端获得的fryingRecipeSO时间可能有延迟，所以要在这里判断fryingRecipeSO是否为null，下面burningProgress_OnValueChanged同理
        float fryingProgressMax= fryingRecipeSO!=null?fryingRecipeSO.fryingProgressMax:1f;
        OnProgressBarUIChanged?.Invoke(this, new IHasProgress.OnProgressBarUIChangedEventArgs
        {
            progressNormalized = fryingProgress.Value / fryingProgressMax
        });
    }
    private void BurningProgress_OnValueChanged(float previousValue, float newValue)
    {
        float burningProgressMax = burningRecipeSO != null ? burningRecipeSO.burningProgressMax : 1f;
        OnProgressBarUIChanged?.Invoke(this, new IHasProgress.OnProgressBarUIChangedEventArgs
        {
            progressNormalized = burningProgress.Value / burningProgressMax
        });
    }
    private void State_OnValueChanged(State previousValue, State newValue)
    {
        OnStoveVisualChange?.Invoke(this, new OnStoveVisualChangeEventArgs { state = state.Value } );
        //把处于Idle或Burning状态下的进度条消除也放到这里来
        if(state.Value==State.Idle||state.Value==State.Burning)
        {
            OnProgressBarUIChanged?.Invoke(this, new IHasProgress.OnProgressBarUIChangedEventArgs
            {
                progressNormalized = 0f
            });
        }
    }
    private void Update()
    {
        //使用NetworkVariable同步时间与状态了，所以Update只在Server上执行
        if(!IsServer)
        {
            return;
        }
        switch(state.Value)
        {
            case State.Idle:
                break;
            case State.Frying:
                fryingProgress.Value+=Time.deltaTime;
                if(fryingProgress.Value>= fryingRecipeSO.fryingProgressMax)
                {
                    //Frying complete
                    KitchenObjectSO outputFryingRecipeSO = GetOutputFryingRecipeSO(GetKitchenObject().GetKitchenObjectSO());
                    KitchenObject.DestroyKitchenObject(GetKitchenObject());
                    KitchenObject.SpawnKitchenObject(outputFryingRecipeSO, this);
                    SetBurningRecipeClientRpc(GameMultiplayer.Instance.GetKitchenObjectSOIndex(outputFryingRecipeSO));
                    burningProgress.Value = 0f;
                    state.Value = State.Fried;
                }
                break;
            case State.Fried:
                burningProgress.Value+=Time.deltaTime;
                if(burningProgress.Value>= burningRecipeSO.burningProgressMax)
                {
                    //Burning complete
                    KitchenObjectSO outputBurningRecipeSO = GetOutputBurningRecipeSO(GetKitchenObject().GetKitchenObjectSO());
                    KitchenObject.DestroyKitchenObject(GetKitchenObject());
                    KitchenObject.SpawnKitchenObject(outputBurningRecipeSO, this);
                    state.Value = State.Burning;
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
                    //为了防止直接setOwner导致的同步问题，先定义一个新变量来得到player的KitchenObject
                    KitchenObject kitchenObject=player.GetKitchenObject();
                    kitchenObject.SetOwner(this);
                    //因为同步问题，所以不能直接GetKitchenObject().GetKitchenObjectSO()，而是通过GameMultiplayer.Instance.GetKitchenObjectSOIndex(kitchenObject.GetKitchenObjectSO())来获取索引，再通过ServerRpc来设置fryingRecipeSO
                    InteractLogicPlaceObjectServerRpc(GameMultiplayer.Instance.GetKitchenObjectSOIndex(kitchenObject.GetKitchenObjectSO()));
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
                if(player.GetKitchenObject().TryGetPlate(out PlateKitchenObject plateKitchenObject))
                {
                    //Player手上是盘子
                    if (plateKitchenObject.TryAddIngredient(GetKitchenObject().GetKitchenObjectSO()))
                    {
                        KitchenObject.DestroyKitchenObject(GetKitchenObject());
                        SetStateIdleServerRpc();
                    }
                }
            }
            else
            {
                //Player没有物体
                GetKitchenObject().SetOwner(player);
                SetStateIdleServerRpc();
            }
        }
    }
    //针对中途拿走的情况，把状态重置为Idle，但state只能在Server上修改，所以要用ServerRpc
    [ServerRpc(RequireOwnership = false)]
    public void SetStateIdleServerRpc()
    {
        state.Value = State.Idle;
    }

    //本来应该是同步玩家Interact柜台的逻辑，但很多都用NetworkVariable同步了
    [ServerRpc(RequireOwnership = false)]
    public void InteractLogicPlaceObjectServerRpc(int KitchenObjectSOIndex)
    {
        fryingProgress.Value = 0f;
        state.Value = State.Frying;
        SetFryingRecipeClientRpc(KitchenObjectSOIndex);
    }
    //这两个ClientRpc是为了让客户端也能知道fryingRecipeSO和burningRecipeSO的值，方便客户端显示进度条
    [ClientRpc]
    public void SetFryingRecipeClientRpc(int KitchenObjectSOIndex)
    {
        KitchenObjectSO kitchenObjectSO = GameMultiplayer.Instance.GetKitchenObjectSOFromIndex(KitchenObjectSOIndex);
        fryingRecipeSO = GetFryingRecipeSO(kitchenObjectSO);
    }
    [ClientRpc]
    public void SetBurningRecipeClientRpc(int KitchenObjectSOIndex)
    {
        KitchenObjectSO kitchenObjectSO = GameMultiplayer.Instance.GetKitchenObjectSOFromIndex(KitchenObjectSOIndex);
        burningRecipeSO = GetBurningRecipeSO(kitchenObjectSO);
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
    //获取是否处于已烹饪状态
    public bool IsFried()
    {
        return state.Value == State.Fried;
    }

}
