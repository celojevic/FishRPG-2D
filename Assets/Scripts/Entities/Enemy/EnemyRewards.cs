using FishNet.Object;
using UnityEngine;

[RequireComponent(typeof(Enemy))]
public class EnemyRewards : ItemDropper 
{

    // TODO options: drops for killer only, drops for anyone
    public ItemReward[] ItemRewards;

    /// <summary>
    /// The master Enemy script.
    /// </summary>
    private Enemy _enemy;

    private void Awake()
    {
        _enemy = GetComponent<Enemy>();
    }

    private void Start()
    {
        _enemy.GetVital(VitalType.Health).OnDepleted += EnemyRewards_OnDepleted;
    }

    private void OnDestroy()
    {
        _enemy.GetVital(VitalType.Health).OnDepleted -= EnemyRewards_OnDepleted;
    }

    private void EnemyRewards_OnDepleted()
    {
        SpawnItemDrop(new ItemValue 
        { 
            Item = ItemRewards[0].Item, 
            Quantity = ItemRewards[0].Quantity 
        }, transform.position);
    }

}

[System.Serializable]
public class ItemReward
{
    public ItemBase Item;
    public int Quantity;
    public float Chance;

    // TODO cumulative and regular options
}