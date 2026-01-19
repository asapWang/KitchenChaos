using System;
using UnityEngine;

public class PlatesCounter : BaseCounter
{
    //如果让柜子不断生成盘子实体的话，柜子只会有一个子盘子，所以先前生成的盘子都会拿不了
    //所以改为生成和销毁盘子visualPrefab来表现盘子的增减，真正拿盘子的话就像ContainerCounter一样，直接生成一个盘子实体给玩家
    [SerializeField] private KitchenObjectSO kitchenObjectSOPlate;
    private float spawnPlateTimer;
    private float spawnPlateTimerMax = 4f;
    public event EventHandler OnPlateSpawned;
    public event EventHandler OnPlateRemoved;
    private void Update()
    {
        spawnPlateTimer += Time.deltaTime;
        if (spawnPlateTimer >= spawnPlateTimerMax)
        {
            spawnPlateTimer = 0f;
            OnPlateSpawned?.Invoke(this, EventArgs.Empty);
        }
    }
    public override void Interact(Player player)
    {
        if (!player.HasKitchenObject())
        {
            KitchenObject.SpawnKitchenObject(kitchenObjectSOPlate, player);
            OnPlateRemoved?.Invoke(this, EventArgs.Empty);
        }
    }

}
