using FishNet.Object;
using UnityEngine;

public class EnemyRewards : MonoBehaviour 
{

    public ItemReward[] ItemRewards;

    // TODO options: drops for killer only, drops for anyone



}

[System.Serializable]
public class ItemReward
{
    public ItemBase Item;
    public int Quantity;
    public float Chance;

    // TODO cumulative and regular options
}