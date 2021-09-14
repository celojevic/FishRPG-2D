using UnityEngine;

[CreateAssetMenu(menuName = "FishRPG/Item/Equipment/Tool")]
public class ToolItem : EquipmentItem
{

    [Header("Tool")]
    public ToolType ToolType;

}


public enum ToolType : byte
{
    Axe,

    Count
}