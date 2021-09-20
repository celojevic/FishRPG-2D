using FishNet.Object;
using System;
using UnityEngine;
using FishRPG.Entities.Player;
using FishRPG.Vitals;

[CreateAssetMenu(menuName = "FishRPG/Item/Consumable/Potion")]
public class PotionItem : ConsumableItem
{

    [Header("Potion")]
    public PotionEffect[] Effects;

    public event Action OnUse;

    [Server]
    public override bool Use(Player user)
    {
        foreach (var item in Effects)
        {
            switch (item.Adjustment)
            {
                case AdjustOp.Add:
                    VitalBase vital = user.GetVital(item.Vital);
                    if (vital && vital.CurrentVital != vital.MaxVital)
                    {
                        vital.Add(item.Amount);
                        PlayerMessageHandler.SendPlayerMsg(user.Owner, MessageType.Action, $"+{item.Amount}", Color.green);
                    }
                    else
                    {
                        Debug.Log("Already at max health!");
                        return false;
                    }
                    break;

                case AdjustOp.Subtract:
                    user.GetVital(item.Vital).Subtract(item.Amount);
                    break;

                default:
                    return false;
            }
        }

        OnUse?.Invoke();
        return true;
    }

    public override string BuildString()
    {
        // custom description
        if (!string.IsNullOrEmpty(Description) && !string.IsNullOrWhiteSpace(Description))
            return Description;

        string s = "";

        for (int i = 0; i < Effects.Length; i++)
        {
            s += (Effects[i].Adjustment == AdjustOp.Add) 
                ? "Heals" : "Removes";

            s += $" {Effects[i].Amount} {Effects[i].Vital}. ";
        }

        return s;
    }

}

[Serializable]
public class PotionEffect
{
    [Tooltip("Vital to adjust.")]
    public VitalType Vital;
    [Tooltip("How to adjust the vital.")]
    public AdjustOp Adjustment;
    [Tooltip("Amount to adjust vital.")]
    public int Amount;
}

public enum AdjustOp
{
    Add,
    Subtract,
}
