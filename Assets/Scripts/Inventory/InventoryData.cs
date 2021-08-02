using UnityEngine;

[CreateAssetMenu(menuName = "FishRPG/Inventory/Data")]
public class InventoryData : ScriptableObject
{

    [Tooltip("Max amount of individual items the inventory can hold.")]
    public ushort MaxSize = 10;

    [Tooltip("Type of items this inventory can hold. If none, it can hold any items.")]
    public InventoryType InvType = InventoryType.None;

}

public enum InventoryType
{
    None,

    KeyItems,
    Weapons,
    Potions,
}
