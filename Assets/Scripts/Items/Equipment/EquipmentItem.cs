using UnityEngine;

[CreateAssetMenu(menuName = "FishRPG/Item/Equipment")]
public class EquipmentItem : ItemBase
{

    [Header("Equipment")]
    public EquipmentSlot Slot;
    public Appearance[] Appearance;
    public StatValue[] Stats;

    public NetEquipment ToNetEquip() => new NetEquipment { ItemBaseGuid = base.Guid };

    public override string BuildString()
    {
        string s = "";
        s += $"{name}\n" +
            //"{IMAGE}\n" +
            $"{Description}\n";

        foreach (var item in Stats)
            s += item.BuildString();

        return s;
    }

    private void OnValidate()
    {
        if (Slot == EquipmentSlot.Count)
        {
            Debug.LogWarning($"Can't set slot to Count. Set a valid slot for {name}.");
            Slot = EquipmentSlot.Weapon;
        }
    }

}

public enum EquipmentSlot : byte
{
    Weapon,

    // keep last
    Count
}
