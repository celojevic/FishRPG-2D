using UnityEngine;

[CreateAssetMenu(menuName = "FishRPG/Item/Equipment")]
public class EquipmentItem : ItemBase
{

    [Header("Equipment")]
    public EquipmentSlot Slot;
    public Appearance[] Appearance;
    public StatValue[] Stats;


}

public enum EquipmentSlot
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
