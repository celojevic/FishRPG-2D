using UnityEngine;

[System.Serializable]
public class StatValue : IStringBuilder
{
    public StatBase Stat;
    public float BaseValue;

    public bool RandomStats;
    public Vector2 ValueRange;

    public string BuildString()
    {
        return $"{Stat.name}: {BaseValue}\n";
    }
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
