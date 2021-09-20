using FishRPG.Entities;
using UnityEngine;

[CreateAssetMenu(menuName = "FishRPG/Spells/Base")]
public abstract class SpellBase : ScriptableObject, IStringBuilder
{

    public Sprite Sprite = null;

    // level req

    [Tooltip("Array of vital costs required to cast spell.")]
    public VitalValue[] Costs = null;
    [Tooltip("How long it takes before the spell is casted.")]
    public float CastTime = 1f;
    [Tooltip("How long before this spell can be casted again.")]
    public float Cooldown = 5f;

    public GameObject CastAnimPrefab = null;

    public abstract string BuildString();

    public abstract void Cast(Entity caster);

}

[System.Serializable]
public class VitalValue
{
    public VitalType Type;
    public int Value;
}
