using UnityEngine;

[CreateAssetMenu(menuName = "FishRPG/Item/Equipment")]
public class EquipmentItem : ItemBase
{

    [Header("Equipment")]
    public EquipmentSlot Slot;
    public Sprite Paperdoll;
    public RuntimeAnimatorController Controller;

}

public enum EquipmentSlot
{
    None,

    Weapon,

    Count
}
