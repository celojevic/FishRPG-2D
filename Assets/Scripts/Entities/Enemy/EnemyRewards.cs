using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class EnemyRewards : ItemDropper 
{

    // TODO options: drops for killer only, drops for anyone
    public ItemReward[] ItemRewards;

    [Server]
    public void DropItems()
    {
        if (!ItemRewards.IsValid()) return;

        SpawnItemDrop(
            new ItemValue(ItemRewards[Random.Range(0, ItemRewards.Length)]),
            transform.position
        );
    }

}

[System.Serializable]
public class ItemReward
{
    public ItemBase Item;
    public int Quantity;
    public float Chance;

    // TODO cumulative and regular options, move itemRewards to base ItmeDropper class
}