using UnityEngine;

[CreateAssetMenu(menuName = "FishRPG/Inventory/Data")]
public class InventoryData : ScriptableObject
{

    [Tooltip("Max amount of individual items the inventory can hold.")]
    public ushort MaxSize = 10;
    
    [Tooltip("Type of items this inventory can hold. If any, it can hold any type of item.")]
    public InventoryType InvType = InventoryType.Any;

    [Tooltip("If assigned, inventory grid layout will automatically be set according to values.")]
    public InventoryUIData UIData = null;

}

public enum InventoryType
{
    Any,

    KeyItems,
    Weapons,
    Potions,
}
