using System;
using NUnit.Framework;
using Unity.Netcode;
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
        if(!IsServer)
        {
            return;
        }
        spawnPlateTimer += Time.deltaTime;
        //加个条件，只有在游戏状态是playing的时候才生成盘子
        if (spawnPlateTimer >= spawnPlateTimerMax && GameManager.Instance.IsPlaying())
        {
            spawnPlateTimer = 0f;
            SpawnPlateClientRpc();
        }
    }
    //同步 生成盘子事件,因为上面的Update只在服务器上执行，所以不用调用ServerRpc来同步了，直接调用ClientRpc就行了
    [ClientRpc]
    public void SpawnPlateClientRpc()
    {
        OnPlateSpawned?.Invoke(this, EventArgs.Empty);
    }
    public override void Interact(Player player)
    {
        if (!player.HasKitchenObject())
        {
            //生成厨房物体已经同步过了
            KitchenObject.SpawnKitchenObject(kitchenObjectSOPlate, player);
            RemovePlateServerRpc();
        }
    }
    //同步 销毁盘子事件
    [ServerRpc(RequireOwnership = false)]
    public void RemovePlateServerRpc()
    {
        RemovePlateClientRpc();
    }
    [ClientRpc]
    public void RemovePlateClientRpc()
    {
        OnPlateRemoved?.Invoke(this, EventArgs.Empty);
    }

}
