using UnityEngine;

[CreateAssetMenu(menuName = "FishRPG/Item/Equipment")]
public class EquipmentItem : ItemBase
{

    [Header("Equipment")]
    public EquipmentSlot Slot;
    public Appearance[] Appearance;
    public StatValue[] Stats;

    public NetEquipment ToNetEquip() => new NetEquipment { ItemBaseGuid = base.Guid };

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

[System.Serializable]
public class StatValue
{
    public StatBase Stat;
    public float BaseValue;

    public bool RandomStats;
    public Vector2 ValueRange;
}

[System.Serializable]
public class RandomStat
{
    public StatValue Stat;

    public float Value;

    public RandomStat()
    {
        Value = Random.Range(Stat.ValueRange.x, Stat.ValueRange.y);
    }
}
