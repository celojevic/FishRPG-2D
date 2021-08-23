using UnityEngine;

[CreateAssetMenu(menuName = "FishRPG/Spells/Damage")]
public class DamageSpell : SpellBase
{

    [Header("Damage Spells")]
    // TODO calc damage based on flat, percent, more, and add with stat
    //      spell levels...
    public int BaseDamage = 1;
    public float PercentDamageIncrease;

    public float AoeRadius = 1f;
    public float Range = 1f;

    public GameObject OnHitAnimPrefab = null;

    public override void Cast(Entity caster)
    {
        throw new System.NotImplementedException();
    }

    public override string BuildString()
    {
        throw new System.NotImplementedException();
    }

}
