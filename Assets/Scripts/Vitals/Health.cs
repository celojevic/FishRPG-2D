using UnityEngine;

public class Health : VitalBase
{

    public override void Add(int amount)
    {
        base.Add(amount);

        PlayerMessageHandler.SendPlayerMsg(Owner, MessageType.Action, $"+{amount}",
            Color.green, gameObject);
    }

    public override void Subtract(int amount)
    {
        base.Subtract(amount);

        PlayerMessageHandler.SendPlayerMsg(Owner, MessageType.Action, $"-{amount}",
            Color.red, gameObject);
    }

}
